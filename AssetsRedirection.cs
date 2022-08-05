using BepInEx;
using HarmonyLib;
using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using XUnity.ResourceRedirector;

namespace BugFables.AssetsRedirector
{
  [BepInPlugin("com.aldelaro5.BugFables.plugins.AssetsRedirector", "Assets Redirector", "1.0.0")]
  [BepInProcess("Bug Fables.exe")]
  public class AssetsRedirection : BaseUnityPlugin
  {
    public void Awake()
    {
      Common.Plugin = this;
      var harmony = new Harmony("com.aldelaro5.BugFables.plugins.AssetsRedirector");
      harmony.PatchAll(typeof(EntitiesSpritesRedirector));
      harmony.PatchAll(typeof(PlayMusicRedirector));
      ResourceRedirection.EnableSyncOverAsyncAssetLoads();
      ResourceRedirection.RegisterResourceLoadedHook(HookBehaviour.OneCallbackPerResourceLoaded, AseetLoad);
    }

    private void AseetLoad(ResourceLoadedContext context)
    {
      // Only redirect sound effects, music are redirected via Harmony patch
      if (context.Asset is AudioClip && context.Parameters.Path.StartsWith("Audio/Sounds"))
        RedirectSoundEffect(context);
      else if (context.Asset is Sprite)
        RedirectSprites(context);
      else if (context.Asset is TextAsset)
        RedirectTextAsset(context);
      else if (context.Asset is Material)
        RedirectMaterialTexture(context);
    }

    private void RedirectMaterialTexture(ResourceLoadedContext context)
    {
      Material mat = (Material)context.Asset;
      if (mat?.mainTexture?.name == "main1")
      {
        string path = Path.Combine(Path.GetDirectoryName(base.Info.Location), "Sprites\\Misc\\main1");
        if (File.Exists(path + ".png"))
        {
          ImageConversion.LoadImage((Texture2D)mat.mainTexture, File.ReadAllBytes(path + ".png"));
          context.Complete();
        }
      }
      if (mat?.mainTexture?.name == "main2")
      {
        string path = Path.Combine(Path.GetDirectoryName(base.Info.Location), "Sprites\\Misc\\main2");
        if (File.Exists(path + ".png"))
        {
          ImageConversion.LoadImage((Texture2D)mat.mainTexture, File.ReadAllBytes(path + ".png"));
          context.Complete();
        }
      }
    }

    private void RedirectTextAsset(ResourceLoadedContext context)
    {
      string path = Path.Combine(Path.GetDirectoryName(base.Info.Location), context.Parameters.Path);
      string resolved = ResolveTextFilePath(path);
      if (resolved != null)
      {
        context.Asset = new TextAsset(File.ReadAllText(resolved));
        context.Complete();
      }
      else if ((resolved = ResolveTextFilePath(path + ".template")) != null)
      {
        string processed;
        string[] bfAsset = Resources.Load<TextAsset>(context.Parameters.Path).ToString().Replace("\r\n", "\n").Split('\n');
        // process template file
        string[] templateText = File.ReadAllLines(resolved);
        foreach (string line in templateText)
        {
          string beforeSpace, afterSpace;
          int firstSpaceInd, lineNum;

          firstSpaceInd = line.IndexOf(' ');
          if (firstSpaceInd < 0)
            continue;
          beforeSpace = line.Substring(0, firstSpaceInd);
          afterSpace = line.Substring(firstSpaceInd + 1);

          if (int.TryParse(beforeSpace, out lineNum))
            bfAsset[lineNum] = afterSpace;
        }

        // cache modified file
        processed = string.Join("\n", bfAsset);
        File.WriteAllText(path, processed);
        context.Asset = new TextAsset(processed);
        context.Complete();
      }
    }

    private string ResolveTextFilePath(string path)
    {
      if (File.Exists(path))
        return path;
      else if (File.Exists(path + ".txt"))
        return path + ".txt";
      else if (File.Exists(path + ".bytes"))
        return path + ".bytes";
      return null;
    }

    private void RedirectSprites(ResourceLoadedContext context)
    {
      string path = Path.Combine(Path.GetDirectoryName(base.Info.Location), context.Parameters.Path);
      if (File.Exists(path + ".png"))
      {
        Sprite ogSprite = (Sprite)context.Asset;
        ImageConversion.LoadImage(ogSprite.texture, File.ReadAllBytes(path + ".png"));
        Vector2 standardisedPivot = new Vector2(ogSprite.pivot.x / ogSprite.rect.width, ogSprite.pivot.y / ogSprite.rect.height);
        Sprite newSprite = Sprite.Create(ogSprite.texture, ogSprite.rect, standardisedPivot, ogSprite.pixelsPerUnit);
        newSprite.name = ogSprite.name;
        context.Asset = newSprite;
        context.Complete();
      }
    }

    private void RedirectSoundEffect(ResourceLoadedContext context)
    {
      string path = Path.Combine(Path.GetDirectoryName(base.Info.Location), context.Parameters.Path);
      if (File.Exists(path + ".wav"))
      {
        string name = context.Asset.name;
        context.Asset = Common.LoadAudioClip(path + ".wav");
        context.Asset.name = name;
        context.Complete();
      }
      else if (File.Exists(path + ".ogg"))
      {
        string name = context.Asset.name;
        context.Asset = Common.LoadAudioClip(path + ".ogg");
        context.Asset.name = name;
        context.Complete();
      }
    }
  }
}
