using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AssetBundleBuilder : MonoBehaviour {
	
	public string FromPath;
	public string BuiltPath;
	public AssetBundleInfoData Data = new AssetBundleInfoData();
	public AssetBundleInfoData CurData = new AssetBundleInfoData();
	public string ResourcePath = "";
	public int RecourcesPerPack = 20;
	public TextAsset assetBundleInfo;

	public int version;
	public List<AssetBundleInfoData.AssetBundleInfo> AssetBundleInfos;
	public List<AssetBundleInfoData.AssetObjectInfo> AssetObjectInfos;

	
#if UNITY_EDITOR

	public static void AddBuildObject(string ObjectName, string PackName, Object obj, AssetBundleInfoData data){
		data.AddAssetObjectInfo(ObjectName, PackName, obj);
	}
	
//	void ModifyAtlasName(Object o){
//		GameObject go = o as GameObject;
//		if(go != null && go.GetComponent<UIAtlas>() != null)
//		{
//			if(o.name == go.GetComponent<UIAtlas>().spriteMaterial.name)
//			{
//				AssetDatabase.RenameAsset( AssetDatabase.GetAssetPath(o), o.name+"_atlas" );
//				AssetDatabase.SaveAssets();
//				AssetDatabase.Refresh();
//			}
//		}
//	}

	public void GeneratePacks(){
		//gather all files from ResourcePath
		Dictionary<string, Object> ResourceBePacked = new Dictionary<string, Object>();
		string[] textures = Directory.GetFiles( Application.dataPath  + ResourcePath,
		                                    "*.png",
		                                    SearchOption.AllDirectories);

		foreach(string s in textures){
			print (s);
			int idx = s.LastIndexOf("Assets");
			string d = s.Substring(idx, s.Length-idx);
			Object obj = AssetDatabase.LoadAssetAtPath(d, typeof(Texture));
			if(!ResourceBePacked.ContainsKey(obj.name)){
				ResourceBePacked.Add(obj.name, obj );
			}
		}

		textures = Directory.GetFiles(Application.dataPath  + ResourcePath,
		                              "*.PNG",
		                              SearchOption.AllDirectories);
		
		foreach(string s in textures){
			int idx = s.LastIndexOf("Assets");
			string d = s.Substring(idx, s.Length-idx);
			Object obj = AssetDatabase.LoadAssetAtPath(d, typeof(Texture));
			if(!ResourceBePacked.ContainsKey(obj.name)){
				ResourceBePacked.Add(obj.name, obj );
			}
		}

		textures = Directory.GetFiles(Application.dataPath  + ResourcePath,
		                                       "*.jpg",
		                                       SearchOption.AllDirectories);

		foreach(string s in textures){
			int idx = s.LastIndexOf("Assets");
			string d = s.Substring(idx, s.Length-idx);
			Object obj = AssetDatabase.LoadAssetAtPath(d, typeof(Texture));
			if(!ResourceBePacked.ContainsKey(obj.name)){
				ResourceBePacked.Add(obj.name, obj );
			}
		}

		List<Object> l = new List<Object>();
		int curCount = 0;
		int packCount = 0;
		List<Object> ResourceObjectBePacked = ResourceBePacked.Values.ToList();
		while(packCount*RecourcesPerPack + curCount < ResourceBePacked.Count){
			l.Add(ResourceObjectBePacked[packCount*RecourcesPerPack + curCount]);
			++curCount;
			if(curCount == RecourcesPerPack){
				CreatePack(l, "AssetBundlePack" + (packCount+1).ToString());
				++packCount;
				curCount = 0;
				l.Clear();
			}
		}
		if(curCount > 0)
			CreatePack(l, "AssetBundlePack" + (packCount+1).ToString());


		string[] text = Directory.GetFiles(Application.dataPath  + ResourcePath,
		                                   "*.txt",
		                                   SearchOption.AllDirectories);

		// static tables
		l.Clear();
		foreach(string s in text){
			int idx = s.LastIndexOf("Assets");
			string d = s.Substring(idx, s.Length-idx);
			l.Add(AssetDatabase.LoadAssetAtPath(d, typeof(TextAsset)) );
		}
		CreatePack(l, "StaticTable");


		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	public void CreatePack(List<Object> resources, string packName){
		GameObject go = new GameObject();
		go.name = packName;
		go.AddComponent<AssetBundlePack>().objects = resources.ToArray();

		Object pref = AssetDatabase.LoadAssetAtPath("Assets/BuildTool/BuiltPrefabs/" + packName + ".prefab", typeof(GameObject));
		if(pref == null)
			pref = PrefabUtility.CreateEmptyPrefab("Assets/BuildTool/BuiltPrefabs/" + packName + ".prefab");
		PrefabUtility.ReplacePrefab(go, pref, ReplacePrefabOptions.ConnectToPrefab);
		DestroyImmediate( go );
	}
	
	public static void GetbuildObjects(string Frompath, AssetBundleInfoData data){
		string[] paths = Directory.GetFiles(Frompath, "*.prefab");
		//add Object info
		foreach( string s in paths ){
			int idx = s.LastIndexOf("Assets");
			GameObject pref = AssetDatabase.LoadAssetAtPath(s.Substring(idx, s.Length-idx), typeof(Object)) as GameObject;
			AssetBundlePack pack = pref.GetComponent<AssetBundlePack>();
			if(pack != null){
				foreach(Object obj in pack.objects){
					AddBuildObject( obj.name, pack.name, obj, data );
				}
			}
			else{
				AddBuildObject( pref.name, pref.name, pref, data );
			}
		}
	}

	public static void BuildAssetBundlesCMD()
	{
		string[] cmdline = System.Environment.GetCommandLineArgs();

		for (int i=0; i<cmdline.Length; ++i) 
		{
			print (cmdline[i]);
			if(cmdline[i] == "-executeMethod")
			{
				bool fa = System.Convert.ToBoolean (cmdline[i+4]);
				print ("assetbundle building...");
				BuildAssetBundles (EditorUserBuildSettings.activeBuildTarget, cmdline[i+2] + AssetBundleLoader.GetPlatformPath(), cmdline[i+3], fa, new AssetBundleInfoData(), new AssetBundleInfoData());
				print ("assetbundle done!");
				return;
			}

		}
	}
	
	public static void BuildAssetBundles(BuildTarget platform, string infoPath, string objectPath, bool forceBuildAll, AssetBundleInfoData newData, AssetBundleInfoData oldData ){
		newData.ClearData();
		oldData.ClearData();
		//read Asset bundle info file
		oldData.ReadAssetBundleInfoFile( infoPath );
		//gathering all build objects
		GetbuildObjects(objectPath, newData);
		
		//update assetbundle version
		AssetBundleInfoData.AssetBundleInfo[] AssetBundleInfoForBuild = null;
		if(forceBuildAll)
			AssetBundleInfoForBuild = newData.UpdateAll(oldData);
		else
			AssetBundleInfoForBuild = newData.UpdateVersion(oldData);
		
		//for each updated data, build assetbundle
		foreach( AssetBundleInfoData.AssetBundleInfo info in AssetBundleInfoForBuild ){
			List<Object> objects = new List<Object>();
			foreach(string s in info.objs){
				objects.Add(newData.AssetObjectInfos[s].obj);
			}
			BuildPipeline.BuildAssetBundle(objects[0], objects.ToArray(), infoPath + "/" + info.Name + ".unity3d", BuildAssetBundleOptions.CollectDependencies | BuildAssetBundleOptions.CompleteAssets, platform);
		}
		
		//remember the old Assetbundles
		foreach(KeyValuePair<string, AssetBundleInfoData.AssetBundleInfo> pair in oldData.AssetBundleInfos){
			if(!newData.AssetBundleInfos.ContainsKey(pair.Key)){
				newData.AssetBundleInfos.Add(pair.Key, pair.Value);
				foreach(string s in pair.Value.objs){
					if(!newData.AssetObjectInfos.ContainsKey(s)){
						newData.AssetObjectInfos.Add( s, oldData.AssetObjectInfos[s] );
					}
				}
			}
		}
		//write info file
		newData.WriteAssetBundleInfo(infoPath);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
	
#endif
}
