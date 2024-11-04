// Skeleton implementation written by Joe Zachary for CS 3500, September 2013
// Version 1.1 - Joe Zachary
//   (Fixed error in comment for RemoveDependency)
// Version 1.2 - Daniel Kopta Fall 2018
//   (Clarified meaning of dependent and dependee)
//   (Clarified names in solution/project structure)
// Version 1.3 - H. James de St. Germain Fall 2024

using System.Runtime.CompilerServices;

namespace CS3500.DependencyGraph;

/// <summary>
///   <para>
///     (s1,t1) is an ordered pair of strings, meaning t1 depends on s1.
///     (in other words: s1 must be evaluated before t1.)
///   </para>
///   <para>
///     A DependencyGraph can be modeled as a set of ordered pairs of strings.
///     Two ordered pairs (s1,t1) and (s2,t2) are considered equal if and only
///     if s1 equals s2 and t1 equals t2.
///   </para>
///   <remarks>
///     Recall that sets never contain duplicates.
///     If an attempt is made to add an element to a set, and the element is already
///     in the set, the set remains unchanged.
///   </remarks>
///   <para>
///     Given a DependencyGraph DG:
///   </para>
///   <list type="number">
///     <item>
///       If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
///       (The set of things that depend on s.)
///     </item>
///     <item>
///       If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
///       (The set of things that s depends on.)
///     </item>
///   </list>
///   <para>
///      For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}.
///   </para>
///   <code>
///     dependents("a") = {"b", "c"}
///     dependents("b") = {"d"}
///     dependents("c") = {}
///     dependents("d") = {"d"}
///     dependees("a")  = {}
///     dependees("b")  = {"a"}
///     dependees("c")  = {"a"}
///     dependees("d")  = {"b", "d"}
///   </code>
/// </summary>
/// 
public class DependencyGraph
{
    ///all private member variables exist here
    /// DependentsMap is a Dictionary of Strings mapped to a Set of Strings (node, set of dependents) 
    /// ie: node must be calculated BEFORE anything in the set it is mapped to (think of as the "in order" map)
    private Dictionary<string, HashSet<string>> DependencyMap;

    ///DependeesMap is a Dictionary of Strings mapped to a Set of Strings (node, set of dependees)
    /// ie; node cannot be calculated UNTIL everything in the set it is mapped to is calculated (think of as the "backwards" map)
    private Dictionary<string, HashSet<string>> DependeeMap;

    ///size variable is a private member varaible to be used to return in the Size method
    private int memberSize;

    /// <summary>
    ///   Initializes a new instance of the <see cref="DependencyGraph"/> class.
    ///   The initial DependencyGraph is empty.
    /// </summary>
    public DependencyGraph()
    {
        DependencyMap = new Dictionary<string, HashSet<string>>();
        DependeeMap = new Dictionary<string, HashSet<string>>();
        memberSize = 0;
    }

    /// <summary>
    ///     The number of ordered pairs in the DependencyGraph.
    /// </summary>
    public int Size
    {
        get
        {
            return memberSize;
        }
    }

    /// <summary>
    ///   Reports whether the given node has dependents (i.e., other nodes depend on it).
    /// </summary>
    /// <param name="nodeName"> The name of the node.</param>
    /// <returns> true if the node has dependents. </returns>
    public bool HasDependents(string nodeName)
    {
        if (DependencyMap.TryGetValue(nodeName, out HashSet<string>? dependents))
        {
            if (dependents.Count == 0) //this accounts for a set that was made, and then had all items removed
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///   Reports whether the given node has dependees (i.e., depends on one or more other nodes).
    /// </summary>
    /// <returns> true if the node has dependees.</returns>
    /// <param name="nodeName">The name of the node.</param>
    public bool HasDependees(string nodeName)
    {
        if (DependeeMap.TryGetValue(nodeName, out HashSet<string>? dependees))
        {
            if (dependees.Count == 0) //this accounts for a set that was made, and then had all items removed
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///   <para>
    ///     Returns the dependents of the node with the given name.
    ///   </para>
    /// </summary>
    /// <param name="nodeName"> The node we are looking at.</param>
    /// <returns> The dependents of nodeName. </returns>
    public HashSet<string> GetDependents(string nodeName)
    {
        HashSet<string>? dependents;
        if (DependencyMap.TryGetValue(nodeName, out dependents))
        {
            return dependents;
        }
        else //it has no dependents, so an empty set is returned to indicate this
        {
            return new HashSet<string>();
        }
    }

    /// <summary>
    ///   <para>
    ///     Returns the dependees of the node with the given name.
    ///   </para>
    /// </summary>
    /// <param name="nodeName"> The node we are looking at.</param>
    /// <returns> The dependees of nodeName. </returns>
    public HashSet<string> GetDependees(string nodeName)
    {
        HashSet<string>? dependees;
        if (DependeeMap.TryGetValue(nodeName, out dependees))
        {

            return dependees;
        }
        else //it has no dependees, so an empty set is returned to indicate this
        {
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// <para>
    ///    Adds the ordered pair (dependee, dependent), if it doesn't exist.
    /// </para>
    /// <para>
    ///   This can be thought of as: dependee must be evaluated before dependent
    /// </para>
    /// </summary>
    /// <param name="dependee"> the name of the node that must be evaluated first</param>
    /// <param name="dependent"> the name of the node that cannot be evaluated until after dependee</param>
    public void AddDependency(string dependee, string dependent)
    {
        //this will locally and temporarily hold the set of strings that depend on our dependee
        HashSet<string>? dependents;
        //check to see if the exact (dependee, dependent) pair already exists in this graph
        if (DependencyMap.TryGetValue(dependee, out dependents))
        {
            //if we made it this far, then we know that the key(dependee) exists in our dependency map
            //now check if the set of dependents already contains this dependent
            if (dependents.Contains(dependent))
            {
                //this exact pair already exists, return and do nothing
                return;
            }
            else
            {
                //the dependee exists as a key, but this dependent doesnt exist in its mapped set yet, so lets add it
                dependents.Add(dependent);
                //now we must add the dependee to the DependeesMap
                AddDependee(dependee, dependent); //private helper

                memberSize++;
            }

        }
        else //this key(dependee) doesnt exist in the map yet
        {
            //add completely new map entry with this key(dependee)
            DependencyMap.Add(dependee, new HashSet<string> { dependent });
            AddDependee(dependee, dependent);

            memberSize++;
        }
    }



    ///<summary>
    ///<para>
    ///   Private helper method used to update the Dependee Map after creating a Dependency
    ///</para>
    ///</summary>
    /// <param name="dependee"> the name of the node that must be evaluated first</param>
    /// <param name="dependent"> the name of the node that cannot be evaluated until after dependee</param>
    private void AddDependee(string dependee, string dependent)
    {
        //locally and temporarily stores the dependees of the dependent(2nd parameter)
        HashSet<string>? dependees;
        //see if this dependent exists for any other dependee
        if (DependeeMap.TryGetValue(dependent, out dependees))
        {
            //NOTE** we dont need to check if this dependee exists in the set, because
            //this method is only called within the AddDependency method, which will return early if
            //the exact pair already exists.

            dependees.Add(dependee);
        }
        //this means TryGetValue failed, and we dont have this key in the map
        else //BRAND NEW KEY VALUE PAIR
        {
            //add this dependent and a new set of dependees that includes this dependee
            DependeeMap.Add(dependent, new HashSet<string> { dependee });
        }
    }

    /// <summary>
    ///   <para>
    ///     Removes the ordered pair (dependee, dependent), if it exists.
    ///   </para>
    /// </summary>
    /// <param name="dependee"> The name of the node that must be evaluated first</param>
    /// <param name="dependent"> The name of the node that cannot be evaluated until after dependee</param>
    public void RemoveDependency(string dependee, string dependent)
    {
        HashSet<string>? dependents;
        if (DependencyMap.TryGetValue(dependee, out dependents))
        {
            
            if(dependents.Remove(dependent)){ //remove from the value, not the key
                RemoveDependee(dependee, dependent); //mirror this removal in the dependee map

                memberSize--;
            }
        }
    }

    /// <summary>
    /// <para>
    ///     Private helper method removes the cooresponding dependee from the dependees map
    /// </para>
    /// </summary>
    /// <param name="dependee"></param>
    /// <param name="dependent"></param>
    private void RemoveDependee(string dependee, string dependent)
    {
        HashSet<string>? dependees;
        if (DependeeMap.TryGetValue(dependent, out dependees))
        {
            dependees.Remove(dependee);
        }
    }

    /// <summary>
    ///   Removes all existing ordered pairs of the form (nodeName, *).  Then, for each
    ///   t in newDependents, adds the ordered pair (nodeName, t).
    /// </summary>
    /// <param name="nodeName"> The name of the node who's dependents are being replaced </param>
    /// <param name="newDependents"> The new dependents for nodeName</param>
    public void ReplaceDependents(string nodeName, IEnumerable<string> newDependents)
    {
        //first check that the node exists in our dependency graph
        if (DependencyMap.TryGetValue(nodeName, out HashSet<string>? oldDependents))
        {
            foreach (string dependent in oldDependents)
            {
                //remove this dependent, dependee pair from both maps
                RemoveDependency(nodeName, dependent);
            }

            foreach (string dependent in newDependents)
            {
                //add this new nodeName(dependee), dependent pair to both maps, if it doesnt already exist
                AddDependency(nodeName, dependent);
            }
        }
        //ACCOUNT FOR A NON EXISTENT DEPENDENT
        else //this nodeName doesnt exist in our DependencyMap
        {
            foreach (string dependent in newDependents)
            {
                //add this new nodeName(dependee), dependent pair to both maps, if it doesnt already exist
                AddDependency(nodeName, dependent);
            }
        }
    }

    /// <summary>
    ///   <para>
    ///     Removes all existing ordered pairs of the form (*, nodeName).  Then, for each
    ///     t in newDependees, adds the ordered pair (t, nodeName).
    ///   </para>
    /// </summary>
    /// <param name="nodeName"> The name of the node who's dependees are being replaced</param>
    /// <param name="newDependees"> The new dependees for nodeName</param>
    public void ReplaceDependees(string nodeName, IEnumerable<string> newDependees)
    {
        //first check that the node exists in our dependency graph(dependee map)
        if (DependeeMap.TryGetValue(nodeName, out HashSet<string>? oldDependees))
        {
            foreach (string dependee in oldDependees)
            {
                //remove this dependent, dependee pair from both maps
                RemoveDependency(dependee, nodeName);

            }

            foreach (string dependee in newDependees)
            {
                //add this new nodeName(dependee), dependent pair to both maps, if it doesnt already exist
                AddDependency(dependee, nodeName);
            }
        }
        //ACCOUNT FOR A NON EXISTENT DEPENDEE
        else //this nodeName doesnt exist in our DependeeMap
        {
            foreach (string dependee in newDependees)
            {
                //add this new nodeName(dependee), dependent pair to both maps, if it doesnt already exist
                AddDependency(dependee, nodeName);
            }
        }
    }
}