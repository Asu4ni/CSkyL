using ColossalFramework.Math;
using CSkyL.Transform;
namespace CSkyL.UI
{
    public static class OverlayUtil
    {
        public static void RenderArrow(
            RenderManager.CameraInfo cameraInfo, Position tail, Position head, UnityEngine.Color32 color, float size = 0.1f, byte alpha = 200 , bool alphaBlend = true)
        {
            color.a = alpha;
            Segment3 line = new Segment3 { a = tail._AsVec, b = head._AsVec};
            float minY = line.Min().y - 0.1f;
            float maxY = line.Max().y + 0.1f;
            RenderManager.instance.OverlayEffect.DrawSegment(
            cameraInfo,
            color,
            line,
            size,
            0,
            minY: minY,
            maxY: maxY,
            renderLimits: true,
            alphaBlend: alphaBlend);

            UnityEngine.Vector3 dir = tail.DisplacementTo(head)._AsVec3;
            dir = dir.normalized;
            UnityEngine.Vector3 dir90 = new UnityEngine.Vector3(dir.z, dir.y, -dir.x);
            Segment3 line1 = new Segment3(line.b, line.b - dir + dir90);
            Segment3 line2 = new Segment3(line.b, line.b - dir - dir90);
            RenderManager.instance.OverlayEffect.DrawSegment(
                cameraInfo,
                color,
                segment1: line1,
                segment2: line2,
                size: size,
                dashLen: 0,
                minY: minY,
                maxY: maxY,
                renderLimits: true,
                alphaBlend: alphaBlend);
        }

        public static void RenderCircle(
            RenderManager.CameraInfo cameraInfo, Position pos, UnityEngine.Color32 color, float size, byte alpha = 200, bool alphaBlend = true)
        {
            color.a = alpha;
            RenderManager.instance.OverlayEffect.DrawCircle(
                cameraInfo,
                color,
                pos._AsVec, size,
                pos.up - 0.1f, pos.up + 0.1f,
                renderLimits: true, alphaBlend: alphaBlend);
        }
    }
}
