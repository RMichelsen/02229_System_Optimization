#pragma once

// Program to print BFS traversal from a given
// source vertex. BFS(int s) traverses vertices
// reachable from s.
#include <string>
#include <vector>
#include <unordered_map>
#include <queue>
#include "xmlReader.h"


using namespace std;

typedef unordered_map<string, Edge> edge_map_t;
typedef unordered_map<string, Flow> flow_map_t;
typedef unordered_map<string, vector<vector<string>>> flow_paths_t;
typedef vector<vector<string>> paths_t;

// This class represents a directed graph using
// adjacency list representation
class Graph
{
    //int V;    // No. of vertices
    //string name;

    // Pointer to an array containing adjacency
    // lists
    unordered_map<string,vector<string>> adj;
public:
    //Graph(int V);  // Constructor

    // function to add an edge to graph
    void addEdge(string src, string dest);

    // prints BFS traversal from a given source s
    paths_t BFS(string src, string dest);
private:
    bool nodeVisitedInPath(string node, vector<string> path);
    paths_t ConvertPathsNames(paths_t paths);
};

//Graph::Graph(int V)
//{
//    this->V = V;
//}

void Graph::addEdge(string s, string t)
{
    adj[s].push_back(t); // Add dest to src’s adjacency.
    adj[t].push_back(s); // Add src to dest’s adjacency (bi directional).
}

//we do not need visited, but rather visited_in_path()

paths_t Graph::BFS(string src, string dest)
{
    // Create a collection of paths to return
    paths_t paths;
    // Create a queue for BFS
    queue<vector<string>> queue;
    vector<string> firstPath{ src };

    // Mark the current node as visited and enqueue it
    queue.push(firstPath);

    // 'i' will be used to get all adjacent
    // vertices of a vertex
    vector<string>::iterator i;
    vector<string> path;
    vector<string> newPath;
    string node;

    while (!queue.empty())
    {
        // Dequeue a vertex from queue and print it
        path = queue.front();
        queue.pop();

        node = path[path.size() - 1];

        if (node == dest) {
            paths.push_back(path);
            continue;
        }

        // Get all adjacent vertices of the dequeued
        // vertex s. If a adjacent has not been visited in the current path, 
        // make new path, add node and add to queue
        for (i = adj[node].begin(); i != adj[node].end(); ++i)
        {
            if (nodeVisitedInPath(*i, path)) continue;
            newPath = path;
            newPath.push_back(*i);
            queue.push(newPath);
        }
    }

    return ConvertPathsNames(paths);
}

bool Graph::nodeVisitedInPath(string node, vector<string> path)
{
    vector<string>::iterator i;
    for (i = path.begin(); i != path.end(); ++i)
    {
        if (node == *i) {
            return true;
        }
    }

    return false;
}

paths_t Graph::ConvertPathsNames(paths_t paths)
{
    paths_t converted;

    for (const auto &path : paths) {
        vector<string> pathWithNewNames;

        for (int i = 1; i < path.size(); i++)
        {
            pathWithNewNames.push_back(path[i - 1] + path[i]);
        }
        converted.push_back(pathWithNewNames);
    }
    
    return converted;
}


//int test(edge_map_t edges, flow_map_t flows)
//{
//    Graph g;
//    for (auto i = edges.begin(); i != edges.end(); i++)
//    {
//        Edge e = i->second;
//        g.addEdge(e.src, e.dest);
//    }
//
//    for (auto i = flows.begin(); i != flows.end(); i++) {
//        Flow f = i->second;
//        paths_t paths = g.BFS(f.source, f.destination);
//    }
//
//    return 1;
//}

int tryFindPaths(string src, string dest, flow_paths_t flow_paths, paths_t& paths)
{
    for (auto i = flow_paths.begin(); i != flow_paths.end(); i++)
    {
        paths = i->second;
        if (src == paths[0][0] && dest == paths[0][paths[0].size() - 1]) {
            return 1;
        }

        //if (dest == paths[0][0] && src == paths[0][paths.size() - 1])
        //{
        //    //do some reverse stuff
        //    return 1;
        //}
    }

    return 0;
}

flow_paths_t getFlowPaths(edge_map_t edges, flow_map_t flows)
{
    Graph g;
    for (auto i = edges.begin(); i != edges.end(); i++)
    {
        Edge e = i->second;
        g.addEdge(e.src, e.dest);
    }

    flow_paths_t flowPaths;
    paths_t paths;
    for (auto i = flows.begin(); i != flows.end(); i++) {
        Flow f = i->second;
        string src = f.source;
        string dest = f.destination;
        if (!tryFindPaths(src, dest, flowPaths, paths)) {
            paths = g.BFS(src, dest);
        }
        flowPaths[i->first] = paths;
    }

    return flowPaths;
}



