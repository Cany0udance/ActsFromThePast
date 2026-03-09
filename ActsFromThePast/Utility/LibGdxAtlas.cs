using Godot;
using MegaCrit.Sts2.Core.Logging;
using FileAccess = System.IO.FileAccess;

namespace ActsFromThePast;

public static class LibGdxAtlas
{
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    private static readonly Dictionary<string, AtlasData> _atlasCache = new();

    public struct TextureRegion
    {
        public Texture2D Texture;
        public Rect2 Region;
    }
    
    public struct RegionInfo
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int OrigWidth;
        public int OrigHeight;
        public int OffsetX;
        public int OffsetY;
        public bool Rotate;
    }

    public static RegionInfo? GetRegionData(string atlasPath, string regionName)
    {
        var atlasData = LoadAtlasData(atlasPath);
        if (!atlasData.Regions.TryGetValue(regionName, out var region))
        {
            return null;
        }
    
        return new RegionInfo
        {
            X = region.X,
            Y = region.Y,
            Width = region.Width,
            Height = region.Height,
            OrigWidth = region.OrigWidth,
            OrigHeight = region.OrigHeight,
            OffsetX = region.OffsetX,
            OffsetY = region.OffsetY,
            Rotate = region.Rotate
        };
    }

    public static TextureRegion? GetRegion(string atlasPath, string regionName)
    {
        var atlasData = LoadAtlasData(atlasPath);
        if (!atlasData.Regions.TryGetValue(regionName, out var region))
        {
            return null;
        }

        var baseTexture = LoadTexture(region.TexturePath);
        if (baseTexture == null)
            return null;

        return new TextureRegion
        {
            Texture = baseTexture,
            Region = new Rect2(region.X, region.Y, region.Width, region.Height)
        };
    }

    private static Texture2D LoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var cached))
            return cached;

        var texture = GD.Load<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        _textureCache[path] = texture;
        return texture;
    }

    private static AtlasData LoadAtlasData(string atlasPath)
    {
        if (_atlasCache.TryGetValue(atlasPath, out var cached))
            return cached;

        var atlasData = ParseAtlasFile(atlasPath);
        _atlasCache[atlasPath] = atlasData;
        return atlasData;
    }

    private static AtlasData ParseAtlasFile(string atlasPath)
{
    var data = new AtlasData();
    using var fileContent = Godot.FileAccess.Open(atlasPath, Godot.FileAccess.ModeFlags.Read);
    if (fileContent == null)
    {
        return data;
    }
    var text = fileContent.GetAsText(true);
    var lines = text.Split('\n');
    var directory = atlasPath.GetBaseDir();
    string currentTexturePath = null;
    string currentRegion = null;
    var currentRegionData = new RegionData();
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i].Trim('\r');
        if (string.IsNullOrWhiteSpace(line))
        {
            if (currentRegion != null)
            {
                currentRegionData.TexturePath = currentTexturePath;
                data.Regions[currentRegion] = currentRegionData;
            }
            currentRegion = null;
            continue;
        }
        // Handle both .png and .jpg texture files
        if (line.EndsWith(".png") || line.EndsWith(".jpg") || line.EndsWith(".jpeg"))
        {
            if (currentRegion != null)
            {
                currentRegionData.TexturePath = currentTexturePath;
                data.Regions[currentRegion] = currentRegionData;
                currentRegion = null;
            }
            currentTexturePath = directory + "/" + line;
            continue;
        }
        if (line.StartsWith("size:") || line.StartsWith("format:") ||
            line.StartsWith("filter:") || line.StartsWith("repeat:"))
            continue;
        if (lines[i].StartsWith("  ") || lines[i].StartsWith("\t"))
        {
            if (currentRegion == null)
                continue;
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
                continue;
            var key = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).Trim();
            switch (key)
            {
                case "xy":
                    var xy = value.Split(',');
                    currentRegionData.X = int.Parse(xy[0].Trim());
                    currentRegionData.Y = int.Parse(xy[1].Trim());
                    break;
                case "size":
                    var size = value.Split(',');
                    currentRegionData.Width = int.Parse(size[0].Trim());
                    currentRegionData.Height = int.Parse(size[1].Trim());
                    break;
                case "orig":
                    var orig = value.Split(',');
                    currentRegionData.OrigWidth = int.Parse(orig[0].Trim());
                    currentRegionData.OrigHeight = int.Parse(orig[1].Trim());
                    break;
                case "offset":
                    var offset = value.Split(',');
                    currentRegionData.OffsetX = int.Parse(offset[0].Trim());
                    currentRegionData.OffsetY = int.Parse(offset[1].Trim());
                    break;
                case "rotate":
                    currentRegionData.Rotate = value == "true";
                    break;
            }
        }
        else
        {
            if (currentRegion != null)
            {
                currentRegionData.TexturePath = currentTexturePath;
                data.Regions[currentRegion] = currentRegionData;
            }
            currentRegion = line;
            currentRegionData = new RegionData();
        }
    }
    if (currentRegion != null)
    {
        currentRegionData.TexturePath = currentTexturePath;
        data.Regions[currentRegion] = currentRegionData;
    }
    return data;
}

    private class AtlasData
    {
        public Dictionary<string, RegionData> Regions = new();
    }

    private class RegionData
    {
        public string TexturePath;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int OrigWidth;
        public int OrigHeight;
        public int OffsetX;
        public int OffsetY;
        public bool Rotate;
    }
}