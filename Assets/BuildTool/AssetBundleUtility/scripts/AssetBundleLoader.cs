#define DEBUG
using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

public class AssetBundleLoader : MonoBehaviour {

	public static string GetPlatformPath()
	{
		#if UNITY_IPHONE || UNITY_STANDALONE_OSX
		return "ios/";
		#elif UNITY_ANDROID
		return "android/";
		#else
		return "win/";
		#endif
	}


	static public string 	UrlBase = "";	
	static public string	ResourceBasePath = "OfflineData/";
	
	public AssetBundleInfoData Data = new AssetBundleInfoData();

	#region LoadingTask
	class BundleTaskData
	{
		public Dictionary <string, ObjectTaskData> ObjecstList = new Dictionary<string, ObjectTaskData>();
		public System.Action< string, string> reply; // bundle name, result desc
	}

	class ObjectTaskData
	{
		public System.Action< string, string,  Object> reply; // bundle name, result desc, asset
	}

	Dictionary <string, BundleTaskData> TaskQueue = new Dictionary<string, BundleTaskData> ();

	//return true if need to download
	bool _AddObjectTask(string assetName, System.Action< string, string,  Object> reply)
	{
		AssetBundleInfoData.AssetObjectInfo info;
		if (!Data.AssetObjectInfos.TryGetValue (assetName, out info)) 
		{
			Debug.LogError(assetName + " is not in AssetBundleObjects");
			return false;
		}

		string packname = info.PackName.First();

		BundleTaskData btd;
		if (!TaskQueue.TryGetValue (packname, out btd))
		//new to download
		{
			print(packname + "downloading...");
			btd = new BundleTaskData ();
			ObjectTaskData otd = new ObjectTaskData ();
			otd.reply += reply;
			btd.ObjecstList.Add (assetName, otd);
			TaskQueue.Add (packname, btd);
			return true;
		} 
		else 
		//downloading
		{
			ObjectTaskData otd;
			if(!btd.ObjecstList.TryGetValue(assetName, out otd))
			{
				print ("add " + assetName);
				otd = new ObjectTaskData ();
				otd.reply += reply;
				btd.ObjecstList.Add (assetName, otd);
				return false;
			}
			else
			{
				otd.reply += reply;
				return false;
			}
		}
	}

	#endregion
	
	static AssetBundleLoader _instance;
	public static AssetBundleLoader Instance{
		get{
			if(_instance == null){
				GameObject go = new GameObject("AssetBundleLoader");
				_instance = go.AddComponent<AssetBundleLoader>();
				DontDestroyOnLoad(go);
			}
			return _instance;
		}
	}

	public void LoadTask(string key, System.Action<string, string, Object> OnTaskFinish)
	{
		Object ob = Resources.Load(ResourceBasePath + key);
		if (ob != null && OnTaskFinish != null) 
			OnTaskFinish ( key, "success", ob );

		if (_AddObjectTask (key, OnTaskFinish))
			StartCoroutine (_GetBundleOrAsset(key, ""));

	}

	public void LoadAllBundle(System.Action Done)
	{
		StartCoroutine(_LoadAllBundle(Done));
	}

	IEnumerator _LoadAllBundle(System.Action Done)
	{
		yield return StartCoroutine( AssetBundleLoader.Instance.UpdateAssetInfo(null) );
		foreach ( KeyValuePair<string, AssetBundleInfoData.AssetBundleInfo> info in Data.AssetBundleInfos ) 
		{
			if(info.Value.isNew)
				yield return StartCoroutine( _GetBundleOrAsset( "", info.Key ) );
		}
		Data.WriteAssetBundleInfo (Application.persistentDataPath + "/");

		if (Done != null)
			Done ();
	}

	IEnumerator _GetBundleOrAsset(string fname, string PackName){

		if (PackName == "") 
		{
			PackName = Data.AssetObjectInfos[fname].PackName.First();
		}

		AssetBundleInfoData.AssetBundleInfo info = Data.AssetBundleInfos[PackName];


		string URL = UrlBase + GetPlatformPath() + info.Name + ".unity3d" /*+ "?t="+Time.time.ToString()+Random.Range(0,10.0f).ToString()*/;
		WWW www;
		bool bOK = false;
		do{	
			www = WWW.LoadFromCacheOrDownload(URL, info.version);
			//www = new WWW(URL);
			yield return www;
			if(!System.String.IsNullOrEmpty(www.error)){
				Debug.LogError( www.error );
				www.Dispose();
				www = null;
				yield return new WaitForSeconds(1.0f);
			}
			else
				bOK = true;
		}while( !(bOK /*&& _checkMD5(www.bytes, Data.AssetBundleInfos[Name].MD5)*/) );

		if (fname != "")
		//load asset
		{
			foreach (KeyValuePair<string, ObjectTaskData> otd in TaskQueue[PackName].ObjecstList) 
			{
				Object obj = www.assetBundle.Load (otd.Key);
				if (otd.Value.reply != null) 
				{
					otd.Value.reply (otd.Key, "success", obj);
				}
			}
		} 
		else
		//download only
		{
			if(TaskQueue.ContainsKey(PackName) && TaskQueue[PackName].reply != null)
				TaskQueue[PackName].reply(PackName, "success");
		}

		www.assetBundle.Unload(false);
		www.Dispose();
		www = null;

		//clear task queue
		if(TaskQueue.ContainsKey(PackName))
			TaskQueue.Remove (PackName);

#if DEBUG
		downloaded += ( PackName + "downloaded\n" );
#endif
	}
	
	public IEnumerator UpdateAssetInfo(System.Action done){
		WWW www;
		bool bOK = false;
		do{
			www = new WWW(UrlBase + GetPlatformPath() + "assetinfo.txt" /*+ "?t="+Time.time.ToString()+Random.Range(0,10.0f).ToString()*/);
			yield return www;
			if(!System.String.IsNullOrEmpty(www.error)){
				Debug.LogError(UrlBase + GetPlatformPath() + "assetinfo.txt" +" AssetInfoUpdate:" + www.error);
				www.Dispose();
				yield return new WaitForSeconds(1.0f);
			}
			else
				bOK = true;
		}while( !bOK );
		Data.compareAssetBundleInfo( Application.persistentDataPath+"/", www.text);
		www.Dispose();

		if (done != null)
			done ();
	}
	
	bool _checkMD5(byte[] data, string md5){
		
		MD5 _md5 = MD5.Create();
		byte[] hash = _md5.ComputeHash(data);
		StringBuilder sBuilder = new StringBuilder();
	
	    // Loop through each byte of the hashed data 
	    // and format each one as a hexadecimal string.
	    for (int i = 0; i < hash.Length; i++)
	    {
	        sBuilder.Append(hash[i].ToString("x2"));
	    }
		return (sBuilder.ToString() == md5);
	}

#if DEBUG
	string downloaded = "";
	void OnGUI()
	{
		if( GUI.Button(new Rect(10, 10, 200, 50 ), "reset info") )
		{
			Data.ResetLocalAssetinfo();
		}

		if( GUI.Button(new Rect(10, 70, 200, 50 ), "reload") )
		{
			LoadAllBundle(null);
			downloaded = "";
		}
		
		
		GUI.Label(new Rect(10, 150, 200, 300), downloaded);
	}
#endif
}