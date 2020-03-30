﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    [Header("Server Settings")]
    [Tooltip("The address of the server that is going to be connected to")]
    public string m_ServerAddress = "127.0.0.1";
    [Tooltip("The port of the server that is going to be connected to")]
    public int m_ServerPort = 58120;
    [Tooltip("The password of the server that is going to be connected to")]
    public string m_ServerPassword = "!kappa3!";

    [Header("Proxy Settings")]
    [Tooltip("The address of the proxy that is going to be connected to")]
    public string m_ProxyAddress;
    [Tooltip("The port of the proxy that is going to be connected to")]
    public int m_ProxyPort;
    [Tooltip("The password of the proxy that is going to be connected to")]
    public string m_ProxyPassword;

    [Header("Network Settings")]
    [Tooltip("How many players can be connected to the server at one time")]
    public int m_MaxPlayers;
    [Tooltip("How the server is being hosted (being connected via proxy or by itself)")]
    public ENetworkConnectionType m_ConnectionType;
    [Tooltip("Is the unity process going to be a client, a server, or both")]
    public ENetworkType m_NetworkType;
    [Tooltip("The available prefabs for networking")]
    public List<GameObject> m_NetworkPrefabs;
    [Tooltip("The prefab that will be spawned when a player connects")]
    public GameObject m_PlayerPrefab;

    /// <summary>
    /// A static instance to the always active NetworkManager object
    /// </summary>
    public static NetworkManager Instance;
    private NetworkServer m_Server;
    private NetworkClient m_Client;
    private Dictionary<string, NetworkPlayer> m_Players;
    private Dictionary<int, GameObject> m_GameObjects;

    private void Start()
    {
        m_Players = new Dictionary<string, NetworkPlayer>();
        m_GameObjects = new Dictionary<int, GameObject>();

        if (m_NetworkType == ENetworkType.Server || m_NetworkType == ENetworkType.Mixed)
            m_Server = gameObject.AddComponent<NetworkServer>(); // Create our server
        if (m_NetworkType == ENetworkType.Client || m_NetworkType == ENetworkType.Mixed)
            m_Client = gameObject.AddComponent<NetworkClient>();

        if (m_NetworkType == ENetworkType.Server || m_NetworkType == ENetworkType.Mixed)
            Host();
        if (m_NetworkType == ENetworkType.Client || m_NetworkType == ENetworkType.Mixed)
            Connect("asd");
    }

    /// <summary>
    /// Add's an object to the local network
    /// </summary>
    /// <param name="id">The network id of the object</param>
    /// <param name="obj">The game object itself</param>
    public void AddObject(int id, GameObject obj)
    {
        if (m_GameObjects.ContainsKey(id))
            throw new System.Exception("[Manager] Error, an existing object already exists with ID: " + id);

        m_GameObjects.Add(id, obj);
    }

    /// <summary>
    /// Gets a saved networked object
    /// </summary>
    /// <param name="id">The network id of the object</param>
    /// <returns>A network object, or null if no object exists with the specified id</returns>
    public GameObject GetNetworkedObject(int id)
    {
        if (!m_GameObjects.ContainsKey(id))
        {
            return null; 
        }    

        return m_GameObjects[id];
    }

    /// <summary>
    /// Gets the dictionary of networked objects (id's mapped to game objects)
    /// </summary>
    /// <returns>A dictionary of networked objects</returns>
    public Dictionary<int, GameObject> GetNetworkedObjects()
        => m_GameObjects;

    /// <summary>
    /// Removes an object from the networked list, DOES NOT DESTROY THE GAME OBJECT
    /// </summary>
    /// <param name="id">The network id of the object</param>
    public void RemoveObject(int id)
    {
        if (!m_GameObjects.ContainsKey(id))
            return;

        m_GameObjects.Remove(id);
    }

    /// <summary>
    /// Gets a count of how many objects are saved locally
    /// </summary>
    /// <returns>An integer representing the count</returns>
    public int GetObjectCount()
        => m_GameObjects.Count;

    /// <summary>
    /// Returns the dictionary of players
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, NetworkPlayer> GetPlayers()
        => m_Players;

    /// <summary>
    /// Adds a player to the local network
    /// </summary>
    /// <param name="id">The unique id of the player</param>
    /// <param name="player">A custom NetworkPlayer class</param>
    public void AddPlayer(string id, NetworkPlayer player)
    {
        if (m_Players.ContainsKey(id))
            return;

        m_Players.Add(id, player);
    }

    public NetworkPlayer GetPlayer(string id)
    {
        if (m_Players.ContainsKey(id))
            return m_Players[id];

        return null;
    }

    /// <summary>
    /// Gets the amount of players connected
    /// </summary>
    /// <returns>An integer representing the count</returns>
    public int GetPlayerCount()
        => m_Players.Count;

    /// <summary>
    /// Removes a player from the local list, DOES NOT DESTROY/KICK/BAN THE PLAYER
    /// </summary>
    /// <param name="id">The unique id of the player</param>
    public void RemovePlayer(string id)
    {
        if (!m_Players.ContainsKey(id))
            return;

        m_Players.Remove(id);
    }

    /// <summary>
    /// Retreives an index of the prefab from the networked prefabs list
    /// </summary>
    /// <param name="obj">The prefab being searched for</param>
    /// <returns>An integer representing the index, or -2 if no index is found</returns>
    public int GetIndexByPrefab(GameObject obj)
    {
        if (obj == null)
            throw new System.Exception($"[Manager] The object passed into GetIndexByObject cannot be null");

        int index = -2;
        for(int i = 0; i < m_NetworkPrefabs.Count; i++)
        {
            if (m_NetworkPrefabs[i] == obj)
                index = i;
        }

        if (obj == m_PlayerPrefab)
            index = -1;
        if (index == -2)
            throw new System.Exception($"[Manager] The object {obj} is not a verified network object! Please add it to the network prefab list.");

        return index;
    }

    /// <summary>
    /// Gets an object by the index, using the networked prefabs array
    /// </summary>
    /// <param name="index">The index of the object</param>
    /// <returns>The object itself, or a exception if no object is found</returns>
    public GameObject GetObjectByIndex(int index)
    {
        if (index == -1)
            return m_PlayerPrefab;

        if (index < 0 || index > m_NetworkPrefabs.Count - 1)
            throw new System.Exception("[Manager] Index is not within the range of the prefab list");

        return m_NetworkPrefabs[index];
    }

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.LogWarning("[Manager] A new network manager was created, yet one already exists.");
            return; // We want to use the already existing network manager
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Starts hosting a server with the editor specified address, port and password
    /// </summary>
    public void Host()
        => Host(m_ServerAddress, m_ServerPort, m_ServerPassword);

    /// <summary>
    /// Starts hosting a server given a address, port, and password to host it with
    /// </summary>
    /// <param name="address">The address the server will be hosting on</param>
    /// <param name="port">The port the server will be hosting on</param>
    /// <param name="password">The password used to connect to the server. Leave blank if no password is required</param>
    public void Host(string address, int port, string password = "")
    {
        m_Server.Host(address, port, password);
    }

    /// <summary>
    /// Connect to a server with the predefined address and port.
    /// </summary>
    /// <param name="password">The password required for the server. Leave blank if no password is required</param>
    public void Connect(string password = "")
       => Connect(m_ServerAddress, m_ServerPort, password);

    /// <summary>
    /// Connect to a server with a custom address and port (proxy/server)
    /// </summary>
    /// <param name="serverAddress">The IP address of the server we are going to connect to</param>
    /// <param name="serverPort">The port of the server we are going to connect to</param>
    /// <param name="password">The password required for the server. Leave blank if no password is required</param>
    public void Connect(string serverAddress, int serverPort, string password = "")
    {
        m_Client.Connect(serverAddress, serverPort, password);
    }

    public enum ENetworkType
    {
        Server,
        Client,
        Mixed
    }

    public enum ENetworkConnectionType
    {
        Server,
        Proxy
    }
}
