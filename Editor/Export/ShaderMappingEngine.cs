using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Shader映射引擎 - 基于映射表的shader转换系统
/// </summary>
public class ShaderMappingEngine
{
    // 映射规则
    private List<TypeMapping> typeRules = new List<TypeMapping>();
    private List<FunctionMapping> functionRules = new List<FunctionMapping>();
    private List<VariableMapping> variableRules = new List<VariableMapping>();
    private List<PatternMapping> patternRules = new List<PatternMapping>();
    private List<IncludeMapping> includeRules = new List<IncludeMapping>();
    private Dictionary<string, CustomFunction> customFunctions = new Dictionary<string, CustomFunction>();

    // 统计信息
    private int typeReplacementCount = 0;
    private int functionReplacementCount = 0;
    private int variableReplacementCount = 0;
    private int patternReplacementCount = 0;

    /// <summary>
    /// 加载映射表
    /// </summary>
    public bool LoadMappings(string jsonPath)
    {
        try
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"ShaderMappingEngine: Mapping file not found: {jsonPath}");
                return false;
            }

            string json = File.ReadAllText(jsonPath);
            MappingData data = JsonUtility.FromJson<MappingData>(json);

            if (data == null || data.mappings == null)
            {
                Debug.LogError($"ShaderMappingEngine: Failed to parse mapping file: {jsonPath}");
                return false;
            }

            // 加载各类规则并按优先级排序
            if (data.mappings.types != null)
            {
                typeRules.AddRange(data.mappings.types.OrderByDescending(r => r.priority));
            }

            if (data.mappings.functions != null)
            {
                functionRules.AddRange(data.mappings.functions.OrderByDescending(r => r.priority));
            }

            if (data.mappings.variables != null)
            {
                variableRules.AddRange(data.mappings.variables.OrderByDescending(r => r.priority));
            }

            if (data.mappings.patterns != null)
            {
                patternRules.AddRange(data.mappings.patterns.OrderByDescending(r => r.priority));
            }

            if (data.mappings.includes != null)
            {
                includeRules.AddRange(data.mappings.includes);
            }

            if (data.mappings.custom_functions != null)
            {
                foreach (var func in data.mappings.custom_functions)
                {
                    customFunctions[func.name] = func;
                }
            }

            Debug.Log($"ShaderMappingEngine: Loaded {typeRules.Count} type rules, " +
                     $"{functionRules.Count} function rules, " +
                     $"{variableRules.Count} variable rules, " +
                     $"{patternRules.Count} pattern rules from {jsonPath}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ShaderMappingEngine: Error loading mappings from {jsonPath}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 应用所有映射规则
    /// </summary>
    public string ApplyMappings(string code, string context)
    {
        // 重置统计
        typeReplacementCount = 0;
        functionReplacementCount = 0;
        variableReplacementCount = 0;
        patternReplacementCount = 0;

        string result = code;

        // 1. 类型转换
        result = ApplyTypeMappings(result, context);

        // 2. 变量转换
        result = ApplyVariableMappings(result, context);

        // 3. 函数转换
        result = ApplyFunctionMappings(result, context);

        // 4. 正则模式转换
        result = ApplyPatternMappings(result, context);

        // 5. Include转换
        result = ApplyIncludeMappings(result, context);

        return result;
    }

    /// <summary>
    /// 应用类型映射
    /// </summary>
    private string ApplyTypeMappings(string code, string context)
    {
        string result = code;

        foreach (var rule in typeRules)
        {
            if (!IsContextMatch(rule.context, context))
                continue;

            // 使用单词边界确保完整匹配
            string pattern = $@"\b{Regex.Escape(rule.from)}\b";
            int count = Regex.Matches(result, pattern).Count;

            if (count > 0)
            {
                result = Regex.Replace(result, pattern, rule.to);
                typeReplacementCount += count;
            }
        }

        return result;
    }

    /// <summary>
    /// 应用变量映射
    /// </summary>
    private string ApplyVariableMappings(string code, string context)
    {
        string result = code;

        foreach (var rule in variableRules)
        {
            if (!IsContextMatch(rule.context, context))
                continue;

            // 使用单词边界确保完整匹配
            string pattern = $@"\b{Regex.Escape(rule.from)}\b";
            int count = Regex.Matches(result, pattern).Count;

            if (count > 0)
            {
                result = Regex.Replace(result, pattern, rule.to);
                variableReplacementCount += count;
            }
        }

        return result;
    }

    /// <summary>
    /// 应用函数映射（支持参数替换）
    /// </summary>
    private string ApplyFunctionMappings(string code, string context)
    {
        string result = code;

        foreach (var rule in functionRules)
        {
            if (!IsContextMatch(rule.context, context))
                continue;

            int replaced = ApplyFunctionRule(ref result, rule);
            functionReplacementCount += replaced;
        }

        return result;
    }

    /// <summary>
    /// 应用单个函数规则
    /// </summary>
    private int ApplyFunctionRule(ref string code, FunctionMapping rule)
    {
        int replacementCount = 0;

        try
        {
            // 解析from模式，提取函数名和参数占位符
            string funcName = ExtractFunctionName(rule.from);
            if (string.IsNullOrEmpty(funcName))
                return 0;

            int pos = 0;
            while (pos < code.Length)
            {
                int funcStart = code.IndexOf(funcName, pos);
                if (funcStart < 0)
                    break;

                // 检查是否是完整的函数名（前面不是字母数字下划线）
                if (funcStart > 0 && (char.IsLetterOrDigit(code[funcStart - 1]) || code[funcStart - 1] == '_'))
                {
                    pos = funcStart + funcName.Length;
                    continue;
                }

                // 找到左括号
                int parenStart = funcStart + funcName.Length;
                while (parenStart < code.Length && char.IsWhiteSpace(code[parenStart]))
                    parenStart++;

                if (parenStart >= code.Length || code[parenStart] != '(')
                {
                    pos = funcStart + funcName.Length;
                    continue;
                }

                // 提取参数（平衡括号）
                List<string> args = ExtractFunctionArguments(code, parenStart);
                if (args == null)
                {
                    pos = funcStart + funcName.Length;
                    continue;
                }

                // 找到右括号的位置
                int parenEnd = FindMatchingParen(code, parenStart);
                if (parenEnd < 0)
                {
                    pos = funcStart + funcName.Length;
                    continue;
                }

                // 构建替换字符串
                string replacement = rule.to;
                for (int i = 0; i < args.Count; i++)
                {
                    replacement = replacement.Replace($"{{{i}}}", args[i].Trim());
                }

                // 执行替换
                code = code.Substring(0, funcStart) + replacement + code.Substring(parenEnd + 1);

                replacementCount++;
                pos = funcStart + replacement.Length;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ShaderMappingEngine: Error applying function rule '{rule.from}': {e.Message}");
        }

        return replacementCount;
    }

    /// <summary>
    /// 应用正则模式映射
    /// </summary>
    private string ApplyPatternMappings(string code, string context)
    {
        string result = code;

        foreach (var rule in patternRules)
        {
            if (!IsContextMatch(rule.context, context))
                continue;

            try
            {
                int count = Regex.Matches(result, rule.pattern).Count;
                if (count > 0)
                {
                    result = Regex.Replace(result, rule.pattern, rule.replacement);
                    patternReplacementCount += count;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ShaderMappingEngine: Error applying pattern rule '{rule.name}': {e.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// 应用Include映射
    /// </summary>
    private string ApplyIncludeMappings(string code, string context)
    {
        string result = code;

        foreach (var rule in includeRules)
        {
            if (!IsContextMatch(rule.context, context))
                continue;

            if (result.Contains(rule.from))
            {
                // 构建替换文本
                string replacement = string.Join("\n", rule.to.Select(s => $"    {s}"));
                result = result.Replace(rule.from, replacement);
            }
        }

        return result;
    }

    /// <summary>
    /// 检查上下文是否匹配
    /// </summary>
    private bool IsContextMatch(string ruleContext, string currentContext)
    {
        if (string.IsNullOrEmpty(ruleContext) || ruleContext == "all")
            return true;

        return ruleContext.Equals(currentContext, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 提取函数名
    /// </summary>
    private string ExtractFunctionName(string pattern)
    {
        int parenIndex = pattern.IndexOf('(');
        if (parenIndex > 0)
            return pattern.Substring(0, parenIndex).Trim();

        return pattern.Trim();
    }

    /// <summary>
    /// 提取函数参数（平衡括号和逗号）
    /// </summary>
    private List<string> ExtractFunctionArguments(string code, int startParen)
    {
        List<string> args = new List<string>();
        int parenLevel = 0;
        int argStart = startParen + 1;

        for (int i = startParen; i < code.Length; i++)
        {
            char c = code[i];

            if (c == '(')
            {
                parenLevel++;
            }
            else if (c == ')')
            {
                parenLevel--;
                if (parenLevel == 0)
                {
                    // 最后一个参数
                    if (i > argStart)
                    {
                        string arg = code.Substring(argStart, i - argStart).Trim();
                        if (!string.IsNullOrEmpty(arg))
                            args.Add(arg);
                    }
                    return args;
                }
            }
            else if (c == ',' && parenLevel == 1)
            {
                // 参数分隔符
                string arg = code.Substring(argStart, i - argStart).Trim();
                if (!string.IsNullOrEmpty(arg))
                    args.Add(arg);
                argStart = i + 1;
            }
        }

        return null; // 括号不匹配
    }

    /// <summary>
    /// 找到匹配的右括号
    /// </summary>
    private int FindMatchingParen(string code, int startParen)
    {
        int parenLevel = 0;

        for (int i = startParen; i < code.Length; i++)
        {
            if (code[i] == '(')
                parenLevel++;
            else if (code[i] == ')')
            {
                parenLevel--;
                if (parenLevel == 0)
                    return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取转换统计信息
    /// </summary>
    public string GetStatistics()
    {
        return $"Type: {typeReplacementCount}, Function: {functionReplacementCount}, " +
               $"Variable: {variableReplacementCount}, Pattern: {patternReplacementCount}";
    }

    /// <summary>
    /// 获取自定义函数
    /// </summary>
    public CustomFunction GetCustomFunction(string name)
    {
        return customFunctions.ContainsKey(name) ? customFunctions[name] : null;
    }

    /// <summary>
    /// 检查规则是否已加载
    /// </summary>
    public bool HasRules()
    {
        return typeRules.Count > 0 || functionRules.Count > 0 ||
               variableRules.Count > 0 || patternRules.Count > 0;
    }
}

#region 数据结构

[Serializable]
public class MappingData
{
    public string version;
    public string name;
    public string description;
    public Mappings mappings;
}

[Serializable]
public class Mappings
{
    public List<TypeMapping> types;
    public List<FunctionMapping> functions;
    public List<VariableMapping> variables;
    public List<PatternMapping> patterns;
    public List<IncludeMapping> includes;
    public List<CustomFunction> custom_functions;
}

[Serializable]
public class TypeMapping
{
    public string from;
    public string to;
    public string context = "all";
    public int priority = 100;
    public string description;
}

[Serializable]
public class FunctionMapping
{
    public string from;
    public string to;
    public string context = "all";
    public int priority = 100;
    public string description;
    public List<string> requires;
}

[Serializable]
public class VariableMapping
{
    public string from;
    public string to;
    public string context = "all";
    public int priority = 100;
    public string description;
}

[Serializable]
public class PatternMapping
{
    public string name;
    public string pattern;
    public string replacement;
    public string context = "all";
    public int priority = 90;
    public string description;
}

[Serializable]
public class IncludeMapping
{
    public string from;
    public List<string> to;
    public string context = "all";
    public int priority = 100;
}

[Serializable]
public class CustomFunction
{
    public string name;
    public string code;
    public string description;
    public int priority = 50;
}

#endregion
