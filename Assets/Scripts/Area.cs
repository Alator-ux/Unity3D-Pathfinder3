using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class Link
{
    public NavMeshLink navMeshLink;
    public bool linkStart;

    public Vector3 Position
    {
        get
        {
            var position = navMeshLink.gameObject.transform.position;
            position += linkStart ? navMeshLink.startPoint : navMeshLink.endPoint;
            return position;
        }
    }
}

public class Area : MonoBehaviour
{
    [SerializeField]
    int index;
    public int Index { get => index; }

    [SerializeField]
    List<Area> neigbours;

    public List<Area> Neigbours { get => neigbours; }

    [SerializeField]
    List<Link> links;

    Dictionary<Area, int> neigbourToIndex = new();

    private void Awake()
    {
        for(var i = 0; i < neigbours.Count; i++)
        {
            neigbourToIndex[neigbours[i]] = i;
        }
    }

    public bool Equals(Area other)
    {
        return other.Index == Index;
    }

    public Vector3 GetLinkPosition(int index)
    {
        return links[index].Position;
    }

    public bool GetLinkPosition(Area other, out Vector3 linkPosition)
    {
        if (neigbourToIndex.ContainsKey(other))
        {
            linkPosition = GetLinkPosition(neigbourToIndex[other]);
            return true;
        }
        linkPosition = Vector3.zero;
        return false;
    }

}
