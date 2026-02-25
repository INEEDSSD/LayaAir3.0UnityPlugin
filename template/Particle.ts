import { FTypeDescriptor } from "../datas/TypeDescriptor";

export const types: FTypeDescriptor[] = [
    {
        name: "Gradient",
        init: {
            _alphaElements: [0, 1, 1, 1, 0, 0, 0, 0],
            _colorAlphaKeysCount: 2,
            _rgbElements: [0, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0],
            _colorRGBKeysCount: 2
        },
        inspector: "gradient",
        options: {
            maxColorNum: 8
        },
        properties: [
            {
                name: "_mode",
                type: "number",
                default: 0,
            },
            {
                name: "_alphaElements",
                type: "Float32Array",
            },
            {
                name: "_colorAlphaKeysCount",
                type: "number",
            },
            {
                name: "_rgbElements",
                type: "Float32Array",
            },
            {
                name: "_colorRGBKeysCount",
                type: "number",
            },
        ]
    },
    {
        name: "GradientDataNumber",
        init: {
            _elements: [0, 0, 1, 0, 0, 0, 0, 0],
            _currentLength: 4,
            _curveMin: -1,
            _curveMax: 1
        },
        inspector: "curve",
        properties: [
            {
                name: "_elements",
                type: "Float32Array",
            },
            {
                name: "_currentLength",
                type: "number",
            },
            {
                name: "_curveMin",
                type: "number",
            },
            {
                name: "_curveMax",
                type: "number",
            }
        ],
        options: {
            isCurve: true,
            isWeight: true
        }
    },
    {
        name: "GradientDataInt",
        init: {
            _elements: [0, 0, 1, 1, 0, 0, 0, 0],
            _currentLength: 4,
            _curveMin: 0,
            _curveMax: 1
        },
        inspector: "curve",
        properties: [
            {
                name: "_elements",
                type: "Float32Array",
            },
            {
                name: "_currentLength",
                type: "number",
            },
            {
                name: "_curveMin",
                type: "number",
            },
            {
                name: "_curveMax",
                type: "number",
            }
        ]
    },
    {
        name: "Burst",
        init: {
            _minCount: 30,
            _maxCount: 30,
        },
        properties: [
            {
                name: "_time",
                type: "number",
                range: [0, Infinity],
                default: 0,
            },
            {
                name: "_minCount",
                type: "number",
                range: [0, Infinity],
                default: 0,
            },
            {
                name: "_maxCount",
                type: "number",
                range: [0, Infinity],
                default: 0,
            }
        ],
    },

    {
        name: "BaseShape",
        init: { enable: true },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: true,
            },
        ]
    },

    {
        name: "BoxShape",
        base: "BaseShape",
        properties: [
            {
                name: "length",
                inspector: "vec3",
                fractionDigits: 2,
                step: 0.1,
                options: {
                    members: ["x", "y", "z"]
                },
            },
            {
                name: "x",
                type: "number",
                default: 1,
                inspector: null,
            },
            {
                name: "y",
                type: "number",
                default: 1,
                inspector: null
            },
            {
                name: "z",
                type: "number",
                default: 1,
                inspector: null
            },
            {
                name: "randomDirection",
                type: "number",
                default: 0,
                inspector: "boolean",
            }
        ]
    },

    {
        name: "CircleShape",
        base: "BaseShape",
        properties: [
            {
                name: "radius",
                type: "number",
                range: [0.001, Infinity],
                default: 1,
                step: 0.1,
                fractionDigits: 2,
            },
            {
                name: "emitFromEdge",
                type: "boolean",
                default: false,
            },
            {
                name: "arcDEG",
                type: "number",
                default: 360,
                range: [0, 360],
                step: 1,
            },
            {
                name: "randomDirection",
                type: "number",
                default: 0,
                inspector: "boolean",
            }
        ]
    },

    {
        name: "ConeShape",
        base: "BaseShape",
        properties: [
            {
                name: "angleDEG",
                type: "number",
                default: 25,
                range: [0, 90],
                step: 1,
            },
            {
                name: "radius",
                type: "number",
                default: 1.0,
                range: [0.001, Infinity],
                step: 0.1,
                fractionDigits: 1,
            },
            {
                name: "length",
                type: "number",
                default: 5.0,
                step: 0.1,
                fractionDigits: 1,
            },
            {
                name: "emitType",
                type: "number",
                enumSource: [
                    { name: "Base", value: 0 },
                    { name: "BaseShell", value: 1 },
                    { name: "Volume", value: 2 },
                    { name: "VolumeShell", value: 3 },
                ],
                default: 0,
            },
            {
                name: "randomDirection",
                type: "number",
                default: 0,
                inspector: "boolean",
            }
        ]
    },

    {
        name: "HemisphereShape",
        base: "BaseShape",
        properties: [
            {
                name: "radius",
                type: "number",
                default: 1.0,
                step: 0.1,
                fractionDigits: 1,
            },
            {
                name: "emitFromShell",
                type: "boolean",
                default: false,
            },
            {
                name: "randomDirection",
                type: "number",
                default: 0,
                inspector: "boolean",
            }
        ]
    },

    {
        name: "SphereShape",
        base: "BaseShape",
        properties: [
            {
                name: "radius",
                type: "number",
                default: 1.0,
                step: 0.1,
                fractionDigits: 1,
            },
            {
                name: "emitFromShell",
                type: "boolean",
                default: false,
            },
            {
                name: "randomDirection",
                type: "number",
                default: 0,
                inspector: "boolean",
            }
        ]
    },

    {
        name: "GradientVelocity",
        init: {
            _constant: { x: 0, y: 0, z: 0 },
            _constantMin: { x: 0, y: 0, z: 0 },
            _constantMax: { x: 0, y: 0, z: 0 }
        },
        properties: [
            {
                name: "_type",
                type: "number",
                enumSource: [
                    { name: "i18n:Constant", value: 0 },
                    { name: "i18n:Curve", value: 1 },
                    { name: "i18n:Random Between Two Constant", value: 2 },
                    { name: "i18n:Random Between Two Curve", value: 3 },
                ],
            },
            {
                name: "_constant",
                type: "Vector3",
                addIndent: 1,
                hidden: "data._type != 0",
            },
            {
                name: "_constantMin",
                type: "Vector3",
                caption: "Min",
                addIndent: 1,
                hidden: "data._type != 2",
            },
            {
                name: "_constantMax",
                type: "Vector3",
                caption: "Max",
                addIndent: 1,
                hidden: "data._type != 2",
            },
            {
                name: "_gradientX",
                type: "GradientDataNumber",
                caption: "X",
                addIndent: 1,
                hidden: "data._type != 1",
                nullable: false,
                options: {
                    curveMin: -1,
                    curveMax: 1
                }
            },
            {
                name: "_gradientY",
                type: "GradientDataNumber",
                caption: "Y",
                addIndent: 1,
                hidden: "data._type != 1",
                nullable: false,
                options: {
                    curveMin: -1,
                    curveMax: 1
                }
            },
            {
                name: "_gradientZ",
                type: "GradientDataNumber",
                caption: "Z",
                addIndent: 1,
                hidden: "data._type != 1",
                nullable: false,
                options: {
                    curveMin: -1,
                    curveMax: 1
                }
            },
            {
                name: "_gradientXMin",
                type: "GradientDataNumber",
                caption: "X Min",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientYMin",
                type: "GradientDataNumber",
                caption: "Y Min",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientZMin",
                type: "GradientDataNumber",
                caption: "Z Min",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientXMax",
                type: "GradientDataNumber",
                caption: "X Max",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientYMax",
                type: "GradientDataNumber",
                caption: "Y Max",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientZMax",
                type: "GradientDataNumber",
                caption: "Z Max",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
            },
        ]
    },
    {
        name: "VelocityOverLifetime",
        init: { enable: true },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: false,
            },
            {
                name: "_velocity",
                type: "GradientVelocity",
                nullable: false,
                hideHeader: true
            },
            {
                name: "space",
                type: "number",
                enumSource: ["i18n:local", "i18n:world"],
            }
        ]
    },
    {
        name: "GradientColor",
        init: {
            _constant: { x: 1, y: 1, z: 1, w: 1 },
            _constantMin: { x: 1, y: 1, z: 1, w: 1 },
            _constantMax: { x: 1, y: 1, z: 1, w: 1 }
        },
        properties: [
            {
                name: "_type",
                type: "number",
                enumSource: [
                    { name: "i18n:Constant", value: 0 },
                    { name: "i18n:Gradient", value: 1 },
                    { name: "i18n:Random Between Two Constant", value: 2 },
                    { name: "i18n:Random Between Two Gradient", value: 3 },
                ],
            },
            {
                name: "_constant",
                type: "Vector4",
                addIndent: 1,
                hidden: "data._type != 0",
                inspector: "color",
            },
            {
                name: "_constantMin",
                type: "Vector4",
                caption: "Min",
                addIndent: 1,
                inspector: "color",
                hidden: "data._type != 2",
            },
            {
                name: "_constantMax",
                type: "Vector4",
                caption: "Max",
                addIndent: 1,
                inspector: "color",
                hidden: "data._type != 2",
            },
            {
                name: "_gradient",
                type: "Gradient",
                addIndent: 1,
                hidden: "data._type != 1",
                nullable: false,
                options: {
                    maxColorNum: 8,
                    maxAlphaNum: 8
                }
            },
            {
                name: "_gradientMin",
                type: "Gradient",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
                options: {
                    maxColorNum: 8,
                    maxAlphaNum: 8
                }
            },
            {
                name: "_gradientMax",
                type: "Gradient",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
                options: {
                    maxColorNum: 8,
                    maxAlphaNum: 8
                }
            }
        ]
    },
    {
        name: "ColorOverLifetime",
        init: { enable: true },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: false,
            },
            {
                name: "_color",
                type: "GradientColor",
                nullable: false,
                hideHeader: true,

            }
        ]
    },
    {
        name: "GradientSize",
        init: {
            _type: 1,
            _constantMin: 1,
            _constantMax: 1,
            _constantMinSeparate: { x: 1, y: 1, z: 1 },
            _constantMaxSeparate: { x: 1, y: 1, z: 1 },
        },
        properties: [
            {
                name: "_separateAxes",
                caption: "3D",
                type: "boolean",
                default: false,
            },
            {
                name: "_type",
                type: "number",
                enumSource: [
                    { name: "i18n:Curve", value: 0 },
                    { name: "i18n:Random Between Two Constant", value: 1 },
                    { name: "i18n:Random Between Two Curve", value: 2 },
                ],
                default: 0,
            },
            {
                name: "_constantMin",
                type: "number",
                caption: "Min",
                addIndent: 1,
                range: [0, Infinity],
                hidden: "data._separateAxes || data._type != 1",
                default: 0,
            },
            {
                name: "_constantMax",
                type: "number",
                caption: "Max",
                addIndent: 1,
                range: [0, Infinity],
                hidden: "data._separateAxes || data._type != 1",
                default: 0,
            },
            {
                name: "_constantMinSeparate",
                type: "Vector3",
                caption: "Min",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 1",
            },
            {
                name: "_constantMaxSeparate",
                type: "Vector3",
                caption: "Max",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 1",
            },
            {
                name: "_gradient",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 0",
                nullable: false,

            },
            {
                name: "_gradientX",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 0",
                nullable: false,
            },
            {
                name: "_gradientY",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 0",
                nullable: false,
            },
            {
                name: "_gradientZ",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 0",
                nullable: false,
            },
            {
                name: "_gradientMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientXMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientYMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientZMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientXMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientYMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
                nullable: false,
            },
            {
                name: "_gradientZMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
                nullable: false,
            }
        ]
    },
    {
        name: "SizeOverLifetime",
        init: { enable: true },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: false,
            },
            {
                name: "_size",
                type: "GradientSize",
                nullable: false,
                hideHeader: true
            }
        ]
    },
    {
        name: "GradientAngularVelocity",
        init: {
            _constant: 45 / 180 * Math.PI,
            _constantMin: 45 / 180 * Math.PI,
            _constantMax: 45 / 180 * Math.PI,
            _constantSeparate: { x: 0, y: 0, z: 45 / 180 * Math.PI },
            _constantMinSeparate: { x: 0, y: 0, z: 45 / 180 * Math.PI },
            _constantMaxSeparate: { x: 0, y: 0, z: 45 / 180 * Math.PI },
        },
        properties: [
            {
                name: "_separateAxes",
                caption: "3D",
                type: "boolean",
                default: false,
            },
            {
                name: "_type",
                type: "number",
                enumSource: [
                    { name: "i18n:Constant", value: 0 },
                    { name: "i18n:Curve", value: 1 },
                    { name: "i18n:Random Between Two Constant", value: 2 },
                    { name: "i18n:Random Between Two Curve", value: 3 },
                ],
                default: 0,
            },
            {
                name: "_constant",
                type: "number",
                hidden: "data._separateAxes || data._type != 0",
            },
            {
                name: "_constantMin",
                type: "number",
                caption: "Min",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 2",
            },
            {
                name: "_constantMax",
                type: "number",
                caption: "Max",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 2",
            },
            {
                name: "_constantSeparate",
                type: "Vector3",
                caption: "Constant",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 0",
            },
            {
                name: "_constantMinSeparate",
                type: "Vector3",
                caption: "Min",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
            },
            {
                name: "_constantMaxSeparate",
                type: "Vector3",
                caption: "Max",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 2",
            },
            {
                name: "_gradient",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 1",
                nullable: false,
            },
            {
                name: "_gradientX",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 1",
                nullable: false,
            },
            {
                name: "_gradientY",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 1",
                nullable: false,
            },
            {
                name: "_gradientZ",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 1",
                nullable: false,
            },
            {
                name: "_gradientMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientXMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientYMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientZMin",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientXMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientYMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 3",
                nullable: false,
            },
            {
                name: "_gradientZMax",
                type: "GradientDataNumber",
                addIndent: 1,
                hidden: "!data._separateAxes || data._type != 3",
                nullable: false,
            },

        ]
    },
    {
        name: "RotationOverLifetime",
        init: { enable: true },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: false,
            },
            {
                name: "_angularVelocity",
                type: "GradientAngularVelocity",
                nullable: false,
                hideHeader: true
            }
        ]
    },
    {
        name: "FrameOverTime",
        init: {
            _constant: 1,
            _constantMin: 1,
            _constantMax: 1,
        },
        properties: [
            {
                name: "_type",
                type: "number",
                enumSource: [
                    { name: "i18n:Constant", value: 0 },
                    { name: "i18n:Curve", value: 1 },
                    { name: "i18n:Random Between Two Constant", value: 2 },
                    { name: "i18n:Random Between Two Curve", value: 3 },
                ],
                default: 0,
                onChange: "onTypeChanged",
            },
            {
                name: "_constant",
                type: "number",
                addIndent: 1,
                hidden: "data._type != 0",
                default: 0,
            },
            {
                name: "_constantMin",
                type: "number",
                caption: "Min",
                addIndent: 1,
                hidden: "data._type != 2",
                default: 0,
            },
            {
                name: "_constantMax",
                type: "number",
                caption: "Max",
                addIndent: 1,
                hidden: "data._type != 2",
                default: 0,
            },
            {
                name: "_overTime",
                type: "GradientDataInt",
                addIndent: 1,
                hidden: "data._type != 1",
                nullable: false,
                options: {
                    min: 0,
                }
            },
            {
                name: "_overTimeMin",
                type: "GradientDataInt",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
                options: {
                    min: 0,
                }
            },
            {
                name: "_overTimeMax",
                type: "GradientDataInt",
                addIndent: 1,
                hidden: "data._type != 3",
                nullable: false,
                options: {
                    min: 0,
                }
            }
        ]
    },
    {
        name: "StartFrame",
        properties: [
            {
                name: "_type",
                type: "number",
                enumSource: [
                    { name: "i18n:Constant", value: 0 },
                    { name: "i18n:Random Between Two Constant", value: 1 },
                ],
                default: 0,
            },
            {
                name: "_constant",
                type: "number",
                step: 1,
                addIndent: 1,
                // range: [0, "parent.tiles.x*parent.tiles.y"],
                hidden: "data._type != 0",
                default: 0,
            },
            {
                name: "_constantMin",
                type: "number",
                caption: "Min",
                addIndent: 1,
                step: 1,
                // range: [0, "parent.tiles.x*parent.tiles.y"],
                hidden: "data._type != 1",
                default: 0,
            },
            {
                name: "_constantMax",
                type: "number",
                caption: "Max",
                addIndent: 1,
                step: 1,
                // range: [0, "parent.tiles.x*parent.tiles.y"],
                hidden: "data._type != 1",
                default: 0,
            },
        ]
    },
    {
        name: "TextureSheetAnimation",
        init: {
            enable: true,
            _frame: {
                _type: 0,
                _constant: 1,
                _constantMin: 1,
                _constantMax: 1,
                _overTime: { _elements: [0, 1, 1, 1], _currentLength: 2, _curveMin: 0, _curveMax: 1 },
                _overTimeMin: { _elements: [0, 1, 1, 1], _currentLength: 2, _curveMin: 0, _curveMax: 1 },
                _overTimeMax: { _elements: [0, 1, 1, 1], _currentLength: 2, _curveMin: 0, _curveMax: 1 },
            },
            _startFrame: {
                _type: 0,
                _constant: 0,
                _constantMin: 0,
                _constantMax: 0,
            },
        },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: false,
            },
            {
                name: "tiles",
                type: "Vector2",
                range: [0, Infinity],
                step: 1,
                default: { x: 1, y: 1 },
            },
            {
                name: "type",
                caption: "Animation",
                type: "number",
                enumSource: [
                    { name: "i18n:Whole Sheed", value: 0 },
                    { name: "i18n:Singal Row", value: 1 }
                ],
                default: 0,
            },
            {
                name: "rowIndex",
                type: "number",
                default: 0,
                hidden: "data.type != 1",
            },
            {
                name: "_frame",
                type: "FrameOverTime"
            },
            {
                name: "_startFrame",
                type: "StartFrame"
            },
            {
                name: "cycles",
                type: "number",
                range: [0.1, Infinity],
                default: 1,
            }
        ]
    },

    {
        name: "Emission",
        init: { enable: true },
        properties: [
            {
                name: "enable",
                type: "boolean",
                default: true,
            },
            {
                name: "emissionRate",
                caption: "Rate over Time",
                type: "number",
                range: [0, Infinity],
                default: 10,
            },
            {
                name: "emissionRateOverDistance",
                caption: "Rate over Distance",
                type: "number",
                range: [0, Infinity],
                default: 0,
            },
            {
                name: "_bursts",
                type: ["Burst"],
                nullable: false,
                elementProps: {
                    nullable: false
                },
                default: [],
            }
        ],
    },
    {
        name: "ShurikenParticleSystem",
        caption: "Particle System",
        init: {
            maxParticles: 100,
            startSizeConstant: 0.5,
            shape: {
                _$type: "SphereShape"
            }
        },
        properties: [
            {
                name: "tabBar",
                inspector: "TabBar",
                options: {
                    tabs: {
                        "General": { caption: "i18n:particalTabBar.General", items: ["duration~randomSeed"] },
                        "Emission": { caption: "i18n:particalTabBar.Emission", items: ["emission"] },
                        "Shape": { caption: "i18n:particalTabBar.Shape", items: ["shape"] },
                        "Lifetime": { caption: "i18n:particalTabBar.Lifetime", items: ["velocityOverLifetime", "colorOverLifetime", "sizeOverLifetime", "rotationOverLifetime"] },
                        "TextureSheet": { caption: "i18n:particalTabBar.TextureSheet", items: ["textureSheetAnimation"] },
                    }
                }
            },
            {
                name: "_isPlaying",
                type: "boolean",
                inspector: null
            },
            {
                name: "duration",
                type: "number",
                range: [0.05, Infinity],
                default: 5,
            },
            {
                name: "looping",
                type: "boolean",
                default: true,
            },
            {
                name: "playOnAwake",
                type: "boolean",
                default: true,
            },
            {
                name: "startDelayType",
                caption: "Start Delay",
                type: "number",
                enumSource: [{ name: "i18n:Constant", value: 0 }, { name: "i18n:Random Between Two Constant", value: 1 }],
                default: 0,
            },
            {
                name: "startDelay",
                type: "number",
                caption: "Constant",
                addIndent: 1,
                hidden: "data.startDelayType != 0",
                min: 0,
                default: 0,
            },
            {
                name: "startDelayMin",
                caption: "Min",
                type: "number",
                addIndent: 1,
                hidden: "data.startDelayType != 1",
                default: 0,
            },
            {
                name: "startDelayMax",
                type: "number",
                caption: "Max",
                addIndent: 1,
                hidden: "data.startDelayType != 1",
                default: 0,
            },
            {
                name: "startLifetimeType",
                type: "number",
                caption: "Start Lifetime",
                enumSource: [{ name: "i18n:Constant", value: 0 }, { name: "i18n:Random Between Two Constant", value: 2 }],
                default: 0,
            },
            {
                name: "startLifetimeConstant",
                caption: "Constant",
                type: "number",
                addIndent: 1,
                hidden: "data.startLifetimeType != 0",
                range: [0.0001, Infinity],
                default: 5,
            },
            {
                name: "startLifetimeConstantMin",
                caption: "Min",
                type: "number",
                addIndent: 1,
                hidden: "data.startLifetimeType != 2",
                range: [0.0001, Infinity],
                default: 0,
            },
            {
                name: "startLifetimeConstantMax",
                caption: "Max",
                type: "number",
                addIndent: 1,
                hidden: "data.startLifetimeType != 2",
                range: [0.0001, Infinity],
                default: 5,
            },
            {
                name: "startSpeedType",
                type: "number",
                caption: "Start Speed",
                enumSource: [{ name: "i18n:Constant", value: 0 }, { name: "i18n:Random Between Two Constant", value: 2 }],
                default: 0,
            },
            {
                name: "startSpeedConstant",
                caption: "Constant",
                type: "number",
                addIndent: 1,
                hidden: "data.startSpeedType != 0",
                default: 5,
            },
            {
                name: "startSpeedConstantMin",
                caption: "Min",
                type: "number",
                addIndent: 1,
                hidden: "data.startSpeedType != 2",
                default: 0,
            },
            {
                name: "startSpeedConstantMax",
                caption: "Max",
                type: "number",
                addIndent: 1,
                hidden: "data.startSpeedType != 2",
                default: 5,
            },
            {
                name: "startSizeType",
                type: "number",
                caption: "Start Size",
                enumSource: [{ name: "i18n:Constant", value: 0 }, { name: "i18n:Random Between Two Constant", value: 2 }],
                default: 0,
            },
            {
                name: "threeDStartSize",
                caption: "3D",
                type: "boolean",
                addIndent: 1,
                default: false,
            },
            {
                name: "startSizeConstant",
                caption: "Constant",
                type: "number",
                addIndent: 1,
                hidden: "data.threeDStartSize || data.startSizeType != 0",
                range: [0, Infinity],
                default: 1,
            },
            {
                name: "startSizeConstantMin",
                caption: "Min",
                type: "number",
                addIndent: 1,
                hidden: "data.threeDStartSize || data.startSizeType != 2",
                range: [0, Infinity],
                default: 0,
            },
            {
                name: "startSizeConstantMax",
                caption: "Max",
                type: "number",
                addIndent: 1,
                hidden: "data.threeDStartSize || data.startSizeType != 2",
                range: [0, Infinity],
                default: 1,
            },
            {
                name: "startSizeConstantSeparate",
                caption: "Constant",
                type: "Vector3",
                addIndent: 1,
                hidden: "!data.threeDStartSize || data.startSizeType != 0",
                range: [0, Infinity],
            },
            {
                name: "startSizeConstantMinSeparate",
                caption: "Min",
                type: "Vector3",
                addIndent: 1,
                hidden: "!data.threeDStartSize || data.startSizeType != 2",
                range: [0, Infinity],
            },
            {
                name: "startSizeConstantMaxSeparate",
                caption: "Max",
                type: "Vector3",
                addIndent: 1,
                hidden: "!data.threeDStartSize || data.startSizeType != 2",
                range: [0, Infinity],
            },
            {
                name: "startRotationType",
                type: "number",
                caption: "Start Rotation",
                enumSource: [{ name: "i18n:Constant", value: 0 }, { name: "i18n:Random Between Two Constant", value: 2 }],
                default: 0,
            },
            {
                name: "threeDStartRotation",
                caption: "3D",
                type: "boolean",
                addIndent: 1,
                default: false,
            },
            {
                name: "startRotationConstant",
                caption: "Constant",
                type: "number",
                addIndent: 1,
                hidden: "data.threeDStartRotation || data.startRotationType != 0",
                default: 0,
            },
            {
                name: "startRotationConstantMin",
                caption: "Min",
                type: "number",
                addIndent: 1,
                hidden: "data.threeDStartRotation || data.startRotationType != 2",
                default: 0,
            },
            {
                name: "startRotationConstantMax",
                caption: "Max",
                type: "number",
                addIndent: 1,
                hidden: "data.threeDStartRotation || data.startRotationType != 2",
                default: 0,
            },
            {
                name: "startRotationConstantSeparate",
                caption: "Constant",
                type: "Vector3",
                addIndent: 1,
                inspector: null,
                //hidden: "!data.threeDStartRotation || data.startRotationType != 0",
            },
            {
                name: "startRotationConstantSeparate2",
                caption: "Constant",
                type: "Vector3",
                addIndent: 1,
                hidden: "!data.threeDStartRotation || data.startRotationType != 0",
            },
            {
                name: "startRotationConstantMinSeparate",
                caption: "Min",
                type: "Vector3",
                addIndent: 1,
                hidden: "!data.threeDStartRotation || data.startRotationType != 2",
            },
            {
                name: "startRotationConstantMaxSeparate",
                caption: "Max",
                type: "Vector3",
                addIndent: 1,
                hidden: "!data.threeDStartRotation || data.startRotationType != 2",
            },
            {
                name: "randomizeRotationDirection",
                type: "number",
                caption: "Randomize Direction",
                addIndent: 1,
                range: [0, 1],
                default: 0,
            },
            {
                name: "startColorType",
                type: "number",
                caption: "Start Color",
                enumSource: [{ name: "i18n:Color", value: 0 }, { name: "i18n:Two Colors", value: 2 }],
                default: 0,
            },
            {
                name: "startColorConstant",
                caption: "Constant",
                type: "Vector4",
                inspector: "color",
                addIndent: 1,
                hidden: "data.startColorType != 0",
            },
            {
                name: "startColorConstantMin",
                caption: "Min",
                type: "Vector4",
                inspector: "color",
                addIndent: 1,
                hidden: "data.startColorType != 2",
            },
            {
                name: "startColorConstantMax",
                caption: "Max",
                type: "Vector4",
                inspector: "color",
                addIndent: 1,
                hidden: "data.startColorType != 2",
            },
            {
                name: "gravityModifier",
                type: "number",
                default: 0,
            },
            {
                name: "simulationSpace",
                type: "number",
                enumSource: ["i18n:world", "i18n:local"],
                default: 1,
            },
            {
                name: "simulationSpeed",
                type: "number",
                range: [0, Infinity],
                default: 1,
            },
            {
                name: "scaleMode",
                type: "number",
                enumSource: ["i18n:parScaleMode.hiercachy", "i18n:parScaleMode.local", "i18n:parScaleMode.shape"],
                default: 1,
            },
            {
                name: "maxParticles",
                type: "number",
                range: [0, Infinity],
                step: 1,
            },
            {
                name: "autoRandomSeed",
                type: "boolean",
                default: true,
            },
            {
                name: "randomSeed",
                type: "Uint32Array",
                range: [0, Infinity],
                step: 1,
                hidden: "data.autoRandomSeed",
                fixedLength: true,
                default: [0],
            },
            {
                name: "emission",
                type: "Emission",
                writable: false,
                nullable: false,
            },
            {
                name: "shape",
                type: "BaseShape",
                createObjectMenu: ["BaseShape*"],
                structLike: true,
                nullable: false
            },
            {
                name: "velocityOverLifetime",
                type: "VelocityOverLifetime",
                structLike: true,
            },
            {
                name: "colorOverLifetime",
                type: "ColorOverLifetime",
                structLike: true,
            },
            {
                name: "sizeOverLifetime",
                type: "SizeOverLifetime",
                structLike: true,
            },
            {
                name: "rotationOverLifetime",
                type: "RotationOverLifetime",
                structLike: true,
            },
            {
                name: "textureSheetAnimation",
                type: "TextureSheetAnimation",
                structLike: true,
            }
        ]
    },
    {
        name: "ShurikenParticleRenderer",
        requireEngineLibs: ["laya.particle3D"],
        base: "BaseRender",
        menu: "Rendering,0",
        worldType: "3d",
        newNodeName: "Particle",
        caption: "Particle Renderer",
        icon: "~/ui/type-icons/node/Particle3D.svg",
        init: {
            sharedMaterials: [{ _$uuid: "db42ad88-9d69-48e5-8c97-901e33356b69" }]
        },
        properties: [
            {
                name: "renderMode",
                type: "number",
                enumSource: [
                    { name: "Billboard", value: 0 },
                    { name: "Stretch Billboard", value: 1 },
                    { name: "Horizontal Billboard", value: 2 },
                    { name: "Vertical Billboard", value: 3 },
                    { name: "Mesh", value: 4 },
                ],
                default: 0
            },
            {
                name: "stretchedBillboardSpeedScale",
                caption: "Speed Scale",
                type: "number",
                default: 0,
                hidden: "data.renderMode != 1",
            },
            {
                name: "stretchedBillboardLengthScale",
                caption: "Length Scale",
                type: "number",
                default: 2,
                hidden: "data.renderMode != 1",
            },
            {
                name: "mesh",
                type: "Mesh",
                hidden: "data.renderMode != 4"
            },
            {
                name: "sortingFudge",
                caption: "Sort Fudge",
                type: "number",
                step: 1,
                default: 0
            },
            {
                name: "_particleSystem",
                type: "ShurikenParticleSystem",
                catalog: "Particle System",
                catalogOrder: -1,
                hideHeader: true,
                writable: false,
            },
        ]
    }
]