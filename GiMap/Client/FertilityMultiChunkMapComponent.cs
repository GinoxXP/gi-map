using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class FertilityMultiChunkMapComponent : AChunkMapComponent
{
    private new FertilityMapLayer _mapLayer;
    private Vec3d _mouseWorldPos;
    private int _sideLength = 96;
    
    public FertilityMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, FertilityMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
        _mapLayer = mapLayer;
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
        string localizedString;
        try
        {
            localizedString = _mapLayer.LocalizedNameByColor[color];
        }
        catch (KeyNotFoundException)
        {
            localizedString = Lang.Get("na");
        }
        hoverText.Append(localizedString);
    }
}