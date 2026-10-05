using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>String tables, persisted locale selection and fallback, independent from controls.</summary>
public static class GameText
{
    [Serializable] public class Entry { public string key,value; }
    [Serializable] public class Table { public string locale,fontResource;public Entry[] entries; }
    private static readonly Dictionary<string,Table> tables=new();
    private static readonly Dictionary<string,Dictionary<string,string>> strings=new();
    private static string locale;
    public static event Action Changed;
    public static string Locale { get { Ensure();return locale; } }
    public static string FontResource { get { Ensure();return tables[locale].fontResource; } }
    static bool Load(string code)
    {
        if(tables.ContainsKey(code)) return true;
        var asset=Resources.Load<TextAsset>("Localization/"+code);if(!asset) return false;
        var table=JsonUtility.FromJson<Table>(asset.text);
        if(table?.entries==null || table.locale!=code) return false;
        var values=new Dictionary<string,string>();
        foreach(var e in table.entries) values.Add(e.key,e.value);
        tables.Add(code,table);strings.Add(code,values);return true;
    }
    static void Ensure()
    {
        if(locale!=null) return;
        if(!Load("zh-CN")) throw new InvalidOperationException("Missing default localization table");
        string saved=PlayerPrefs.GetString("ColorBlocks.Locale","zh-CN");
        locale=Load(saved)?saved:"zh-CN";
    }
    public static bool SetLocale(string code)
    {
        Ensure();if(string.IsNullOrEmpty(code) || !Load(code)) return false;
        locale=code;PlayerPrefs.SetString("ColorBlocks.Locale",code);PlayerPrefs.Save();Changed?.Invoke();return true;
    }
    public static string Get(string key,params object[] args)
    {
        Ensure();
        if(!strings[locale].TryGetValue(key,out var value) && !strings["zh-CN"].TryGetValue(key,out value)) return "["+key+"]";
        if(args==null || args.Length==0) return value;
        return string.Format(CultureInfo.GetCultureInfo(locale),value,args);
    }
    public static void Bind(Text target,string key,params object[] args)
    {
        var binding=target.GetComponent<LocalizedLabel>();if(!binding) binding=target.gameObject.AddComponent<LocalizedLabel>();
        binding.Bind(key,args);
    }
}
