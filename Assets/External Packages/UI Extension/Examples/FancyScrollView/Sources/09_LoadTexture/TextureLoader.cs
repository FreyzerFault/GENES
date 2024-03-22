/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace UnityEngine.UI.Extensions.Examples.FancyScrollViewExample09
{
    internal static class TextureLoader
    {
        public static void Load(string url, Action<(string Url, Texture Texture)> onSuccess) =>
            Loader.Instance.Load(url, onSuccess);

        private class Loader : MonoBehaviour
        {
            private static Loader instance;
            private readonly Dictionary<string, Texture> cache = new();

            public static Loader Instance => instance ??
#if UNITY_2023_1_OR_NEWER
                (instance = FindFirstObjectByType<Loader>() ??
#else
                                             (instance = FindObjectOfType<Loader>() ??
#endif
                                                         new GameObject(typeof(TextureLoader).Name)
                                                             .AddComponent<Loader>());

            private void OnDestroy()
            {
                foreach (var kv in cache) Destroy(kv.Value);

                instance = null;
            }

            public void Load(string url, Action<(string Url, Texture Texture)> onSuccess)
            {
                if (cache.TryGetValue(url, out var cachedTexture))
                {
                    onSuccess((url, cachedTexture));
                    return;
                }

                StartCoroutine(
                    DownloadTexture(
                        url,
                        result =>
                        {
                            cache[result.Url] = result.Texture;
                            onSuccess(result);
                        }
                    )
                );
            }

            private IEnumerator DownloadTexture(string url, Action<(string Url, Texture Texture)> onSuccess)
            {
                using (var request = UnityWebRequestTexture.GetTexture(url))
                {
                    yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                    if (request.result == UnityWebRequest.Result.ConnectionError)
#else
                    if (request.isNetworkError)
#endif
                    {
                        Debug.LogErrorFormat("Error: {0}", request.error);
                        yield break;
                    }

                    onSuccess(
                        (
                            url,
                            ((DownloadHandlerTexture)request.downloadHandler).texture
                        )
                    );
                }
            }
        }
    }
}