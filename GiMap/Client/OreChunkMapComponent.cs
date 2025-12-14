using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class OreChunkMapComponent : AChunkMapComponent
{
    private new OreMapLayer _mapLayer;
    private Vec3d _chunkWorldPos;
    private Vec2f _viewPos;
    private Vec3d _mouseWorldPos;
    private int _sideLength = 96;
    
    public OreChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, OreMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
        _mapLayer = mapLayer;
        _chunkWorldPos = new Vec3d(baseChunkCord.X * 32, 0.0, baseChunkCord.Y * 32);
        _viewPos = new Vec2f();
        _mouseWorldPos = new Vec3d();
    }
    
    public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        _viewPos.X = args.X - (float)mapElem.Bounds.renderX;
        _viewPos.Y = args.Y - (float)mapElem.Bounds.renderY;
        mapElem.TranslateViewPosToWorldPos(_viewPos, ref _mouseWorldPos);

        if (_mouseWorldPos.X < _chunkWorldPos.X ||
            _mouseWorldPos.X >= _chunkWorldPos.X + _sideLength ||
            _mouseWorldPos.Z < _chunkWorldPos.Z ||
            _mouseWorldPos.Z >= _chunkWorldPos.Z + _sideLength)
            return;
        
        var posInChunk = _mouseWorldPos - _chunkWorldPos;
        var chunk = _pixelsToSet[posInChunk.XInt / 32 + (posInChunk.ZInt / 32) * 3];
        if (chunk == null)
            return;

        var color = chunk[posInChunk.XInt % 32 + (posInChunk.ZInt % 32) * 32];
        foreach (var colorCode in _mapLayer.ColorsByBlockCode)
        {
            if (colorCode.Value == color)
            {
                var localizedString = _mapLayer.LocalizedNameByBlockCode[colorCode.Key];
                hoverText.Append(localizedString);
                return;
            }
        }
    }
}