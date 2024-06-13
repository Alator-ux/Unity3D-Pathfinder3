using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Bot : MonoBehaviour
{
    [SerializeField]
    Transform endTargetTransform;

    NavMeshAgent agent;

    Task<List<(NavMeshPath, Vector3)>> pathWaiter;
    List<(NavMeshPath, Vector3)> pathSequence;

    Vector3 startPosition;
    int currentPathIndex = 0;
    bool targetReached = false;
    bool waiting = false;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        pathWaiter = GetPathToTarget();
        startWaitingTime = Time.time;
    }
    float startWaitingTime;
    float timeToWait = 1f;
    bool waitForNavmeshOwnerInitialization = false;
    void Update()
    {
        if (pathWaiter != null)
        {
            if (pathWaiter.IsCompleted)
            {
                pathSequence = pathWaiter.Result;
                pathWaiter = null;
            }
            return;
        }
        if(waitForNavmeshOwnerInitialization && Time.time - startWaitingTime < timeToWait)
        {
            return;
        }
        waitForNavmeshOwnerInitialization = false;
        if (agent.hasPath || targetReached)
        {
            return;
        }
        if (waiting)
        {
            var pointForWait = pathSequence[currentPathIndex].Item2;
            NavMeshPath tempPathForWait = new();
            agent.CalculatePath(pointForWait, tempPathForWait);
            if (tempPathForWait.status == NavMeshPathStatus.PathComplete)
            {
                waitForNavmeshOwnerInitialization = true;
                startWaitingTime = Time.time;
                waiting = false;
                currentPathIndex+=2;
                agent.SetPath(tempPathForWait);
            }
            return;
        }
        if(pathSequence == null || currentPathIndex == pathSequence.Count)
        {
            pathWaiter = GetPathToTarget();
            return;
        }
        var path = pathSequence[currentPathIndex].Item1;
        if (path != null)
        {
            if (path.status != NavMeshPathStatus.PathComplete)
            {
                pathWaiter = GetPathToTarget();
                return;
            }
            else
            {
                agent.SetPath(path);
                currentPathIndex++;
            }
            return;
        }

        var point = pathSequence[currentPathIndex].Item2;
        NavMeshPath tempPath = new();
        agent.CalculatePath(point, tempPath);
        if (tempPath.status != NavMeshPathStatus.PathComplete)
        {
            agent.SetPath(new()); // Reset Path
            waiting = true;
        }
        else
        {
            agent.SetPath(tempPath);
            currentPathIndex++;
        }
    }

    float PathDistance(Vector3 source, NavMeshPath path)
    {
        float distance = Vector3.Distance(source, path.corners[0]);
        Vector3 prev = path.corners[0];
        for (var i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(prev, path.corners[i]);
            prev = path.corners[i];
        }
        return distance;
    }

    async Task<List<(NavMeshPath, Vector3)>> GetPathToTarget()
    {
        currentPathIndex = 0;
        startPosition = transform.position;
        NavMeshPath path = new();
        
        var startArea = (agent.navMeshOwner as Component).GetComponent<Area>();

        // Area, Path sequence, is target located in this area
        PriorityQueue<float, (Area, List<(NavMeshPath, Vector3)>, Area, bool)> priorityQueue = new();
        priorityQueue.Enqueue((startArea, new(), null, false), 0);
        
        while (!priorityQueue.IsEmpty())
        {
            // Distance, current area, navmeshpath, previous area, target in this area
            var dCaNmpPaTita = priorityQueue.Dequeue();
            var distance = dCaNmpPaTita.Item1;
            var currentArea = dCaNmpPaTita.Item2.Item1;
            var pathSequence = new List<(NavMeshPath, Vector3)>(dCaNmpPaTita.Item2.Item2);
            var previousArea = dCaNmpPaTita.Item2.Item3;
            var targetInThisArea = dCaNmpPaTita.Item2.Item4;
            if (targetInThisArea)
            {
                return pathSequence;
            }
            Vector3 lastPosition;
            if (pathSequence.Count == 0)
            {
                lastPosition = transform.position;
            }
            else
            {
                var lastPath = pathSequence.Last();
                if (lastPath.Item1 != null)
                {
                    lastPosition = lastPath.Item1.corners.Last();
                }
                else
                {
                    lastPosition = lastPath.Item2;
                }
            }

            path = new();
            NavMesh.CalculatePath(lastPosition, endTargetTransform.position, NavMesh.AllAreas, path);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                var tempDistance = distance + PathDistance(lastPosition, path);
                pathSequence.Add((path, Vector3.zero));
                priorityQueue.Enqueue((currentArea, pathSequence, currentArea, true), tempDistance);
                continue;
            }

            foreach (var area in currentArea.Neigbours)
            {
                if (previousArea != null && area.Equals(previousArea))
                {
                    continue;
                }

                var tempDistance = distance;
                Vector3 linkPosition;
                if (!currentArea.GetLinkPosition(area, out linkPosition))
                {
                    continue;
                }

                path = new();
                NavMesh.CalculatePath(
                    lastPosition,
                    linkPosition, 
                    NavMesh.AllAreas,
                    path);
                if (path.status == NavMeshPathStatus.PathComplete) {
                    tempDistance += PathDistance(lastPosition, path);
                    pathSequence.Add((path, Vector3.zero));
                }
                else {
                    tempDistance += Vector3.Distance(lastPosition, linkPosition);
                    pathSequence.Add((null, linkPosition));
                }

                Vector3 neighbourLinkPosition;
                area.GetLinkPosition(currentArea, out neighbourLinkPosition);
                tempDistance += Vector3.Distance(linkPosition, neighbourLinkPosition);

                pathSequence.Add((null, neighbourLinkPosition));

                priorityQueue.Enqueue((area, pathSequence, currentArea, false), tempDistance);
            }

        }
        return null;
    }

    /*async Task<List<(NavMeshPath, Vector3)>> GetPathToTarget()
    {
        currentPathIndex = 0;
        startPosition = transform.position;
        NavMeshPath path = new();
        NavMesh.CalculatePath(transform.position, endTargetTransform.position, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            var pathSequence = new List<(NavMeshPath, Vector3)>
            {
                (path, Vector3.zero)
            };
            return pathSequence;
        }
        
        var startArea = (agent.navMeshOwner as Component).GetComponent<Area>();

        // Area, Path sequence, is target located in this area
        PriorityQueue<float, (Area, List<(NavMeshPath, Vector3)>, Area, bool)> priorityQueue = new();
        priorityQueue.Enqueue((startArea, new(), null, false), 0);
        
        while (!priorityQueue.IsEmpty())
        {
            // Distance, current area, navmeshpath, previous area, target in this area
            var dCaNmpPaTita = priorityQueue.Dequeue();
            var distance = dCaNmpPaTita.Item1;
            var currentArea = dCaNmpPaTita.Item2.Item1;
            var pathSequence = new List<(NavMeshPath, Vector3)>(dCaNmpPaTita.Item2.Item2);
            var previousArea = dCaNmpPaTita.Item2.Item3;
            var targetInThisArea = dCaNmpPaTita.Item2.Item4;
            Vector3 lastPosition;
            if (pathSequence.Count == 0)
            {
                lastPosition = transform.position;
            }
            else
            {
                var lastPath = pathSequence.Last();
                if (lastPath.Item1 != null)
                {
                    lastPosition = lastPath.Item1.corners.Last();
                }
                else
                {
                    lastPosition = lastPath.Item2;
                }
            }

            NavMesh.CalculatePath(lastPosition, endTargetTransform.position, NavMesh.AllAreas, path);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                pathSequence.Add((path, Vector3.zero));
                return pathSequence;
            }

            var checkedAtLeastOne = false;
            foreach (var area in currentArea.Neigbours)
            {
                if (previousArea != null && area.Equals(previousArea))
                {
                    continue;
                }
                checkedAtLeastOne = true;

                var tempDistance = distance;
                Vector3 linkPosition;
                if (!currentArea.GetLinkPosition(area, out linkPosition))
                {
                    continue;
                }
                NavMeshPath tempPath = new();

                NavMesh.CalculatePath(
                    lastPosition,
                    linkPosition, 
                    NavMesh.AllAreas,
                    path);
                tempDistance += Vector3.Distance(lastPosition, linkPosition);
                pathSequence.Add((null, linkPosition));

                Vector3 neighbourLinkPosition;
                area.GetLinkPosition(currentArea, out neighbourLinkPosition);
                tempDistance += Vector3.Distance(linkPosition, neighbourLinkPosition);

                pathSequence.Add((null, neighbourLinkPosition));

                priorityQueue.Enqueue((area, pathSequence, currentArea, false), tempDistance);
            }
            if (!checkedAtLeastOne)
            {
                NavMeshPath tempPath = new();
                agent.transform.position = lastPosition;
                agent.CalculatePath(endTargetTransform.position, tempPath);
                agent.transform.position = startPosition;
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    pathSequence.Add((path, Vector3.zero));
                    return pathSequence;
                }
            }

        }
        return null;
    }*/
}
