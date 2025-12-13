using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RT.Util;
using RT.Util.Lingo;
using WotDataLib;

namespace TankIconMaker.Layers
{
    sealed class TankImageLayer : LayerBase
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return App.Translation.TankImageLayer.LayerName; } }
        public override string TypeDescription { get { return App.Translation.TankImageLayer.LayerDescription; } }

        public ImageBuiltInStyle Style { get; set; }
        public static MemberTr StyleTr(Translation tr) { return new MemberTr(tr.Category.Image, tr.TankImageLayer.Style); }

        private static string[] GetAllSeparatorVariants(string baseName)
        {
            var parts = Regex.Split(baseName.ToLowerInvariant(), @"[-_]");
            if (parts.Length <= 1) return new[] { baseName.ToLowerInvariant() };

            var separators = new[] { '_', '-' };
            var variants = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < (1 << (parts.Length - 1)); i++)
            {
                var variant = parts[0];
                for (int j = 0; j < parts.Length - 1; j++)
                {
                    variant += separators[i >> j & 1] + parts[j + 1];
                }
                variants.Add(variant);
            }
            return variants.ToArray();
        }

        public override BitmapBase Draw(Tank tank)
        {
            BitmapBase image;
            if (tank is TestTank)
            {
                image = (tank as TestTank).LoadedImage;
            }
            else
            {
                var installation = tank.Context.Installation;
                var config = tank.Context.VersionConfig;
                var guiPackage = config.GuiPackageName.Split(' ', ',', ';');
                switch (Style)
                {
                    case ImageBuiltInStyle.Contour:
                        image = null;
                        foreach (string items in guiPackage)
                        {
                            image = ImageCache.GetImage(new CompositePath(
                                tank.Context, installation.Path,
                                config.PathSourceContour.Replace("\"GuiPackage\"", items),
                                tank.ImageName + config.TankIconExtension));
                            if (image != null)
                                break;
                        }
                        break;
                    case ImageBuiltInStyle.ThreeD:
                        image = null;
                        var imageName3d = tank.ImageName ?? "";
                        var dashIndex3d = imageName3d.IndexOf('-');
                        if (dashIndex3d > 0)
                            imageName3d = imageName3d.Substring(dashIndex3d + 1);
                        
                        var variants3d = GetAllSeparatorVariants(imageName3d);
                        foreach (string items in guiPackage)
                        {
                            var folder3d = config.PathSource3D.Replace("\"GuiPackage\"", items);
                            foreach (var variant in variants3d)
                            {
                                image = ImageCache.GetImage(new CompositePath(
                                    tank.Context,
                                    installation.Path,
                                    folder3d,
                                    variant + config.TankIconExtension));
                                if (image != null)
                                {
                                    tank.Real3DImageName = variant;
                                    Ut.SetReal3DImageName(tank.TankId, variant);
                                    break;
                                }
                            }
                            if (image != null) break;
                        }
                        break;
                    case ImageBuiltInStyle.ThreeDLarge:
                        image = null;
                        var imageName = tank.ImageName ?? "";
                        var dashIndex = imageName.IndexOf('-');
                        if (dashIndex > 0)
                            imageName = imageName.Substring(dashIndex + 1);
                        
                        var variants = GetAllSeparatorVariants(imageName);
                        foreach (string items in guiPackage)
                        {
                            var folder = config.PathSource3DLarge.Replace("\"GuiPackage\"", items);
                            foreach (var variant in variants)
                            {
                                image = ImageCache.GetImage(new CompositePath(
                                    tank.Context,
                                    installation.Path,
                                    folder,
                                    variant + config.TankIconExtension));
                                if (image != null)
                                {
                                    tank.Real3DImageName = variant;
                                    Ut.SetReal3DImageName(tank.TankId, variant);
                                    break;
                                }
                            }
                            if (image != null) break;
                        }
                        break;
                    case ImageBuiltInStyle.Country:
                        if (tank.Country == Country.None)
                            return null;
                        image = null;
                        foreach (string items in guiPackage)
                        {
                            image = ImageCache.GetImage(new CompositePath(
                                tank.Context, installation.Path,
                                config.PathSourceCountry[tank.Country].Replace("\"GuiPackage\"", items)));
                            if (image != null)
                                break;
                        }
                        break;
                    case ImageBuiltInStyle.Class:
                        if (tank.Class == Class.None)
                            return null;
                        image = null;
                        foreach (string items in guiPackage)
                        {
                            image = ImageCache.GetImage(new CompositePath(
                                tank.Context, installation.Path,
                                config.PathSourceClass[tank.Class].Replace("\"GuiPackage\"", items)));
                            if (image != null)
                                break;
                        }
                        break;
                    default:
                        throw new Exception("9174876");
                }
            }

            if (image == null)
            {
                if (tank.TankId != "unknown")
                    tank.AddWarning(App.Translation.TankImageLayer.MissingImageWarning);
                return null;
            }
            return image;
        }
    }

    sealed class CurrentImageLayer : LayerBase
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return App.Translation.CurrentImageLayer.LayerName; } }
        public override string TypeDescription { get { return App.Translation.CurrentImageLayer.LayerDescription; } }

        public override BitmapBase Draw(Tank tank)
        {
            BitmapBase image;
            if (tank is TestTank)
            {
                image = (tank as TestTank).LoadedImage;
            }
            else
            {
                var installation = tank.Context.Installation;
                var config = tank.Context.VersionConfig;
                var guiPackage = config.GuiPackageName.Split(' ', ',', ';');

                image = ImageCache.GetImage(new CompositePath(
                    tank.Context, installation.Path,
                    config.PathDestination,
                    tank.TankId + config.TankIconExtension));

                foreach (string items in guiPackage)
                {
                    image = image ?? ImageCache.GetImage(new CompositePath(
                        tank.Context, installation.Path,
                        config.PathSourceContour.Replace("\"GuiPackage\"", items),
                        tank.TankId + config.TankIconExtension));
                    if (image != null)
                        break;
                }
            }

            if (image == null)
            {
                tank.AddWarning(App.Translation.CurrentImageLayer.MissingImageWarning);
                return null;
            }
            return image;
        }
    }

    sealed class CustomImageLayer : LayerBase
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return App.Translation.CustomImageLayer.LayerName; } }
        public override string TypeDescription { get { return App.Translation.CustomImageLayer.LayerDescription; } }

        public ValueSelector<string> ImageFile { get; set; }
        public static MemberTr ImageFileTr(Translation tr) { return new MemberTr(tr.Category.Image, tr.CustomImageLayer.ImageFile); }

        public CustomImageLayer()
        {
            ImageFile = new ValueSelector<string>("");
        }

        public override LayerBase Clone()
        {
            var result = (CustomImageLayer) base.Clone();
            result.ImageFile = ImageFile.Clone();
            return result;
        }

        public override BitmapBase Draw(Tank tank)
        {
            var filename = ImageFile.GetValue(tank);
            if (string.IsNullOrWhiteSpace(filename))
                return null;

            var image = ImageCache.GetImage(new CompositePath(tank.Context, PathUtil.AppPath, filename));
            if (image == null)
                image = ImageCache.GetImage(new CompositePath(
                    tank.Context,
                    tank.Context.Installation.Path,
                    tank.Context.VersionConfig.PathMods,
                    filename));
            if (image == null)
                image = ImageCache.GetImage(new CompositePath(
                    tank.Context,
                    tank.Context.Installation.Path,
                    filename));

            if (image == null)
            {
                tank.AddWarning(App.Translation.CustomImageLayer.MissingImageWarning.Fmt(filename));
                return null;
            }
            return image;
        }
    }

    sealed class FilenamePatternImageLayer : LayerBase
    {
        public override int Version { get { return 1; } }
        public override string TypeName { get { return App.Translation.FilenamePatternImageLayer.LayerName; } }
        public override string TypeDescription { get { return App.Translation.FilenamePatternImageLayer.LayerDescription; } }

        public string Pattern { get; set; }
        public static MemberTr PatternTr(Translation tr) { return new MemberTr(tr.Category.Image, tr.FilenamePatternImageLayer.Pattern); }

        public FilenamePatternImageLayer()
        {
            Pattern = "Images/Example/tank-{country}-{class}-{category}-{NameShort}.png";
        }

        public override BitmapBase Draw(Tank tank)
        {
            var filename = (Pattern ?? "")
                .Replace("{tier}", tank.Tier.ToString())
                .Replace("{country}", tank.Country.ToString().ToLower())
                .Replace("{class}", tank.Class.ToString().ToLower())
                .Replace("{category}", tank.Category.ToString().ToLower())
                .Replace("{id}", tank.TankId);

            filename = Regex.Replace(filename, @"{([^}]+)}",
                match => tank[match.Groups[1].Value] ?? "");

            if (string.IsNullOrWhiteSpace(filename))
                return null;

            var image = ImageCache.GetImage(new CompositePath(tank.Context, PathUtil.AppPath, filename));
            if (image == null)
                image = ImageCache.GetImage(new CompositePath(
                    tank.Context,
                    tank.Context.Installation.Path,
                    tank.Context.VersionConfig.PathMods,
                    filename));
            if (image == null)
                image = ImageCache.GetImage(new CompositePath(
                    tank.Context,
                    tank.Context.Installation.Path,
                    filename));

            if (image == null)
            {
                tank.AddWarning(App.Translation.FilenamePatternImageLayer.MissingImageWarning.Fmt(filename));
                return null;
            }
            return image;
        }
    }
}