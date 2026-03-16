using UnityEngine;

public class SpaceUtils
{
    private static readonly Quaternion HelpRotation = new Quaternion(0, 1, 0, 0);
    private static Quaternion HelpRotation1 = new Quaternion();
    private static Vector3 HelpVec3 = new Vector3();
    public static void changePostion(ref Vector3 postion)
    {
        postion.x *= -1;
    }
    public static void changePostion(ref float[] postion)
    {
        postion[0] *= -1;
    }

    /// <summary>
    /// 相机/灯光的直接子节点位置转换：只取反Z（而非X）
    /// 因为父级相机/灯光额外施加了Y轴180°旋转，子节点位置需要补偿。
    /// 数学推导：标准转换(取反X) + Y180补偿 = 只取反Z
    /// </summary>
    public static void changePostionForCameraChild(ref Vector3 postion)
    {
        postion.z *= -1;
    }

    public static void changePostionForCameraChild(ref float[] postion)
    {
        postion[2] *= -1;
    }

    public static void changeRotate(ref Quaternion rotation, bool ischange)
    {
        if (ischange)
        {
            // 对相机/灯光：先做Y轴180度旋转（Unity Z-forward → LayaAir Z-backward）
            rotation *= HelpRotation;
            // 然后只翻转w（不翻转x），保持俯仰角方向正确
            rotation.w *= -1;
        }
        else
        {
            // 对普通对象：左手系→右手系转换
            rotation.x *= -1;
            rotation.w *= -1;
        }
    }

    public static void changeRotate(ref float[] rotation, bool ischange)
    {
        HelpRotation1.x = rotation[0];
        HelpRotation1.y = rotation[1];
        HelpRotation1.z = rotation[2];
        HelpRotation1.w = rotation[3];
        changeRotate(ref HelpRotation1, ischange);
        rotation[0] = HelpRotation1.x;
        rotation[1] = HelpRotation1.y;
        rotation[2] = HelpRotation1.z;
        rotation[3] = HelpRotation1.w;
    }

    public static void changeRotateTangle(ref float[] rotation)
    {
        rotation[0] *= -1;
        rotation[3] *= -1;
    }

    /// <summary>
    /// 相机/灯光的直接子节点旋转补偿：对标准转换结果左乘Y180
    /// Y180 * (x,y,z,w) = (z, w, -x, -y)
    /// </summary>
    public static void compensateCameraParentRotation(ref Quaternion rotation)
    {
        float ox = rotation.x, oy = rotation.y, oz = rotation.z, ow = rotation.w;
        rotation.x = oz;
        rotation.y = ow;
        rotation.z = -ox;
        rotation.w = -oy;
    }

    public static void compensateCameraParentRotation(ref float[] rotation)
    {
        float ox = rotation[0], oy = rotation[1], oz = rotation[2], ow = rotation[3];
        rotation[0] = oz;
        rotation[1] = ow;
        rotation[2] = -ox;
        rotation[3] = -oy;
    }

    public static void changeRotateEuler(ref float[] eulr, bool ischange)
    {
        HelpVec3.x = eulr[0];
        HelpVec3.y = eulr[1];
        HelpVec3.z = eulr[2];
        if (ischange)
        {

            HelpRotation1.eulerAngles = HelpVec3;
            HelpRotation1 *= HelpRotation;
            Vector3 angles = HelpRotation1.eulerAngles;
            eulr[0] = -angles.x;  // 对相机的pitch取反，保持向下俯视的效果
            eulr[1] = -angles.y;
            eulr[2] = -angles.z;
        }
        else
        {
            eulr[0] = HelpVec3.x;
            eulr[1] = -HelpVec3.y;
            eulr[2] = -HelpVec3.z;
        }
    }
    public static void changeRotateEulerTangent(ref float[] eulr, bool ischange)
    {
        eulr[1] *= -1;
        eulr[2] *= -1;
    }
}
