using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoadTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		print (Application.persistentDataPath);
		AssetBundleLoader.UrlBase = @"file://F:/AssetbundleTool/Assets/BuildTool/BuiltAssetBundles/";
		//StartCoroutine( AssetBundleLoader.Instance.UpdateAssetInfo( Done ) );
		AssetBundleLoader.Instance.LoadAllBundle (null);
	}

	void Done()
	{
		print ("update assetinfo done");
		AssetBundleLoader.Instance.LoadTask ("a1", finished);
		AssetBundleLoader.Instance.LoadTask ("a2", finished);
		AssetBundleLoader.Instance.LoadTask ("a3", finished);
		AssetBundleLoader.Instance.LoadTask ("4g", finished);
		AssetBundleLoader.Instance.LoadTask ("a2", finished);
		AssetBundleLoader.Instance.LoadTask ("b3", finished);
		AssetBundleLoader.Instance.LoadTask ("4g", finished);
		AssetBundleLoader.Instance.LoadTask ("f2", finished);
		AssetBundleLoader.Instance.LoadTask ("f4", finished);
	}

	void finished(string name, string result, Object obj)
	{
		print (name + " " + result);
	}
	
	// Update is called once per frame
	void Update () {
	
	}


}
