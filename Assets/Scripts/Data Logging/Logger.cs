using SimpleJSON;
using FileAdapter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Basic logger implementation.
public class Logger : MonoBehaviour
{
	private string m_player_id = null;
	private string m_session_id = null;
	private uint m_action_count = 0;

    private Dictionary<string, DateTime> mode_time_starts;
	private IOfunction IO;

    System.DateTime epochStart; 

    public bool IsDevRun = false;
	public string filename;

    // Check if session started.
    public
    bool
	IsSessionBegun()
	{
		return m_session_id != null;
	}

	// Begin the session.
	public
	void
	BeginSession(string game_id, string player_id, string build_id, string version, string condition, JSONClass details)
	{
        if (IsDevRun)
            return;

		IO.Init(filename);
        epochStart = System.DateTime.UtcNow;    //get the start time of the session;

		m_player_id = player_id;
		m_session_id = System.Guid.NewGuid().ToString();
		m_action_count = 0;

		JSONClass data = new JSONClass();
		data["game_id"] = StringOrEmpty(game_id);
		data["player_id"] = StringOrEmpty(m_player_id);
		data["session_id"] = StringOrEmpty(m_session_id);
		data["build_id"] = StringOrEmpty(build_id);
		data["version"] = StringOrEmpty(version); //AR or VR
		data["client_time(s)"] = GetTimestampNow(epochStart).ToString();
		data["details"] = ClassOrEmpty(details);

		JSONClass node = new JSONClass();
		node["type"] = "session_begin";
		node["data"] = data;

		IO.Handle(node);
	}

	// End the current session.
	public
	void
	EndSession(JSONClass details)
	{
        if (IsDevRun)
            return;

		if (m_session_id == null) {
			Debug.Log("No session to end");
		}



		JSONClass data = new JSONClass();
		data["session_id"] = StringOrEmpty(m_session_id);
		data["client_time(s)"] = GetTimestampNow(epochStart).ToString();
		data["details"] = ClassOrEmpty(details);

		JSONClass node = new JSONClass();
		node["type"] = "session_end";
		node["data"] = data;

		m_player_id = null;
		m_session_id = null;
		m_action_count = 0;

		IO.Handle(node);
		IO.Fini();
	}

	// Begin a run.
	public
	void
	AdvanceStep(string name, JSONClass details)
	{
        if (IsDevRun)
            return;

		m_action_count = 0;
		
		JSONClass data = new JSONClass();
		data["session_id"] = StringOrEmpty(m_session_id);
		data["client_time(s)"] = GetTimestampNow(epochStart).ToString();
		data["name"] = "name";
		data["details"] = ClassOrEmpty(details);

		JSONClass node = new JSONClass();
		node["type"] = "step_begin";
		node["data"] = data;

		IO.Handle(node);
	}

	// End the current run.
	public
	void
	EndPreviousStep(JSONClass details)
	{
        if (IsDevRun)
            return;

		JSONClass data = new JSONClass();
		data["action_count"] = m_action_count.ToString();
		data["client_time(s)"] = GetTimestampNow(epochStart).ToString();
		data["details"] = ClassOrEmpty(details);
        data["session_id"] = StringOrEmpty(m_session_id);

		JSONClass node = new JSONClass();
		node["type"] = "step_end";
		node["data"] = data;
		m_action_count = 0;

		IO.Handle(node);
	}

	// Log an action.
	public
	void
	TakeAction(params string[] pairs)
	{
		JSONClass details = new JSONClass ();
		if (pairs.Length % 2 != 0) 
		{
			Debug.LogError ("Taking action: All actions must be passed as pairs of strings");
		} else 
		{
			for (int i = 0; i < pairs.Length; i = i + 2) 
			{
				details.Add ( pairs [i],pairs [i + 1]);
			}
		}

        if (IsDevRun)
            return;

		JSONClass data = new JSONClass();
		data["action_seqno"] = m_action_count.ToString();
		//data["type"] = type.ToString();
		data["client_time(s)"] = GetTimestampNow(epochStart).ToString();
		data["details"] = ClassOrEmpty(details);
        data["session_id"] = StringOrEmpty(m_session_id);

		JSONClass node = new JSONClass();
		node["type"] = "action";
		node["data"] = data;

		++ m_action_count;

		IO.Handle(node);
	}

	private
	string
	StringOrEmpty(string str)
	{
		if (str == null) {
			return "";
		} else {
			return str;
		}
	}
	
	private
	JSONClass
	ClassOrEmpty(JSONClass cls)
	{
		if (cls == null) {
			return new JSONClass();
		} else {
			return cls;
		}
	}

    public 
    double
    TimeSpentonStep(string mode, bool start)
    {
        if(start)
        {
            if (!mode_time_starts.ContainsKey(mode))
                mode_time_starts.Add(mode, System.DateTime.UtcNow);
            else
                mode_time_starts[mode] = System.DateTime.UtcNow;
            return 0;
        }

        if (mode_time_starts.ContainsKey(mode))
            return GetTimestampNow(mode_time_starts[mode]);
        else
            return 0;
    }

	private
	double
	GetTimestampNow(DateTime lastTime)
	{
		//System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		double timestamp = (System.DateTime.UtcNow - lastTime).TotalSeconds;
		return timestamp;
	}
}
