﻿using UnityEngine;
using KBEngine;
using System; 
using System.IO;  
using System.Text;
using System.Collections;

namespace KBEngine
{
	/*
		持久化引擎协议，在检测到协议版本发生改变时会清理协议
	*/
	public class PersistentInofs
	{
		string _persistentDataPath = "";
		string _digest = "";
		
		string _lastloaded = "";
		
	    public PersistentInofs(string path)
	    {
	    	_persistentDataPath = path;
	    	installEvents();
	    }
	        
		void installEvents()
		{
			KBEngine.Event.registerOut("onImportClientMessages", this, "onImportClientMessages");
			KBEngine.Event.registerOut("onImportServerErrorsDescr", this, "onImportServerErrorsDescr");
			KBEngine.Event.registerOut("onImportClientEntityDef", this, "onImportClientEntityDef");
			KBEngine.Event.registerOut("onVersionNotMatch", this, "onVersionNotMatch");
			KBEngine.Event.registerOut("onScriptVersionNotMatch", this, "onScriptVersionNotMatch");
		}
		
		string _getSuffix()
		{
			return _digest + "." + KBEngineApp.app.clientVersion + "." + KBEngineApp.app.clientScriptVersion + "." + 
							KBEngineApp.app.getInitArgs().ip + "." + KBEngineApp.app.getInitArgs().port;
		}
		
		public bool loadAll()
		{
			if(_lastloaded == _getSuffix())
				return true;
			
			KBEngineApp.app.resetMessages();
			
			byte[] loginapp_onImportClientMessages = loadFile (_persistentDataPath, "loginapp_clientMessages." + _getSuffix());

			byte[] baseapp_onImportClientMessages = loadFile (_persistentDataPath, "baseapp_clientMessages." + _getSuffix());

			byte[] onImportServerErrorsDescr = loadFile (_persistentDataPath, "serverErrorsDescr." + _getSuffix());

			byte[] onImportClientEntityDef = loadFile (_persistentDataPath, "clientEntityDef." + _getSuffix());

			if(loginapp_onImportClientMessages.Length > 0 && baseapp_onImportClientMessages.Length > 0)
			{
				try
				{
					if(!KBEngineApp.app.importMessagesFromMemoryStream (loginapp_onImportClientMessages, 
							baseapp_onImportClientMessages, onImportClientEntityDef, onImportServerErrorsDescr))
						
						clearMessageFiles();
						return false;
				}
				catch(Exception e)
				{
					Dbg.ERROR_MSG("PersistentInofs::loadAll(): is error(" + e.ToString() + ")! lastloaded=" + _lastloaded);  
					clearMessageFiles();
					return false;
				}
			}
			
			_lastloaded = _getSuffix();
			return true;
		}
		
		public void onImportClientMessages(string currserver, byte[] stream)
		{
			if(_lastloaded == _getSuffix())
				return;
			
			if(currserver == "loginapp")
				createFile (_persistentDataPath, "loginapp_clientMessages." + _getSuffix(), stream);
			else
				createFile (_persistentDataPath, "baseapp_clientMessages." + _getSuffix(), stream);
		}

		public void onImportServerErrorsDescr(byte[] stream)
		{
			if(_lastloaded == _getSuffix())
				return;
			
			createFile (_persistentDataPath, "serverErrorsDescr." + _getSuffix(), stream);
		}
		
		public void onImportClientEntityDef(byte[] stream)
		{
			if(_lastloaded == _getSuffix())
				return;
			
			createFile (_persistentDataPath, "clientEntityDef." + _getSuffix(), stream);
		}
		
		public void onVersionNotMatch(string verInfo, string serVerInfo)
		{
			clearMessageFiles();
		}

		public void onScriptVersionNotMatch(string verInfo, string serVerInfo)
		{
			clearMessageFiles();
		}
		
		public void onServerDigest(string currserver, string serverProtocolMD5, string serverEntitydefMD5)
		{
			// 我们不需要检查网关的协议， 因为登录loginapp时如果协议有问题已经删除了旧的协议
			if(currserver == "baseapp")
			{
				return;
			}
			
			_digest = serverProtocolMD5 + serverEntitydefMD5;
			
			if(_lastloaded == _getSuffix())
				return;
			
			if(loadFile(_persistentDataPath, serverProtocolMD5 + serverEntitydefMD5 + "." + 
				KBEngineApp.app.getInitArgs().ip + "." + KBEngineApp.app.getInitArgs().port).Length == 0)
			{
				clearMessageFiles();
				createFile(_persistentDataPath, serverProtocolMD5 + serverEntitydefMD5 + "." + 
					KBEngineApp.app.getInitArgs().ip + "." + KBEngineApp.app.getInitArgs().port, new byte[1]);
			}
			else
			{
				loadAll();
			}
		}
			
		public void clearMessageFiles()
		{
			deleteFile(_persistentDataPath, "loginapp_clientMessages." + _getSuffix());
			deleteFile(_persistentDataPath, "baseapp_clientMessages." + _getSuffix());
			deleteFile(_persistentDataPath, "serverErrorsDescr." + _getSuffix());
			deleteFile(_persistentDataPath, "clientEntityDef." + _getSuffix());
			KBEngineApp.app.resetMessages();
			
			_lastloaded = "";
		}
		
		public void createFile(string path, string name, byte[] datas)  
		{  
			deleteFile(path, name);
			Dbg.DEBUG_MSG("createFile: " + path + "/" + name);
			FileStream fs = new FileStream (path + "/" + name, FileMode.OpenOrCreate, FileAccess.Write);
			fs.Write (datas, 0, datas.Length);
			fs.Close ();
			fs.Dispose ();
		}  
	   
	   public byte[] loadFile(string path, string name)  
	   {  
			FileStream fs;

			try{
				fs = new FileStream (path + "/" + name, FileMode.Open, FileAccess.Read);
			}
			catch (Exception e)
			{
				Dbg.DEBUG_MSG("loadFile: " + path + "/" + name);
				Dbg.DEBUG_MSG(e.ToString());
				return new byte[0];
			}

			byte[] datas = new byte[fs.Length];
			fs.Read (datas, 0, datas.Length);
			fs.Close ();
			fs.Dispose ();

			Dbg.DEBUG_MSG("loadFile: " + path + "/" + name + ", datasize=" + datas.Length);
			return datas;
	   }  
	   
	   public void deleteFile(string path, string name)  
	   {  
			Dbg.DEBUG_MSG("deleteFile: " + path + "/" + name);
			
			try{
	        	File.Delete(path + "/"+ name);  
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
	   }  
	}

}
