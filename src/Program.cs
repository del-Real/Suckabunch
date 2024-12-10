﻿/*********************************************************************
* Class Name: Program
* Author/s name: Alberto del Real
* Class description: main entry point for Sokoban game solver with
* graphical visualization of the level using Raylib.
*********************************************************************/

using System;
using Raylib_cs;
using System.Data;
using System.Security;

namespace Sokoban;

class Program {
    public static void Main(string[] args) {

        // Check if there are arguments
        if (args.Length == 0) {
            Console.WriteLine("Usage: ./sokoban.exe <action> <parameters>");
            throw new ArgumentException("No arguments provided");
        }

        // Check if second parameter has a dash
        if (args[1][0] != '-') {
            Console.WriteLine("Dash is required for parameter: -<parameter>");
            throw new ArgumentException("Second parameter must start with a dash.");
        }

        Param param = new Param(args);

        // Valid characters defined
        HashSet<char> validCharacters = new HashSet<char> { '#', '@', '$', '.', '*', '+', ' ', '\n' };

        // Check if all characters in the level string are valid
        foreach (char c in param.Level) {
            if (!validCharacters.Contains(c)) {
                throw new InvalidOperationException("Character not valid: " + c);
            }
        }

        Level level = new Level(param.Level);
        State state = new State(param.Level);
        bool renderActive = false;

        // Check if there is a different number of boxes and targets
        if (state.Boxes.Length > level.Targets.Length) {
            throw new InvalidOperationException("The level must have at least as many targets as boxes");
        }

        // Check if there are not at least one box and one target
        if (state.Boxes.Length == 0 || level.Targets.Length == 0) {
            throw new InvalidOperationException("The level must have at least one box and one target");
        }

        switch (param.Task) {
            case "T1":
                ProblemDomain(level, state);
                break;

            case "T2S":
                List<Successor> successors = SuccessorFunction(level, state);
                Console.WriteLine("ID: " + state.Id);
                foreach (var successor in successors) {
                    successor.PrintSuccessor();
                }
                break;
            case "T2T":
                bool allBoxesOnTarget = ObjectiveFunction(level, state);
                Console.Write(allBoxesOnTarget.ToString().ToUpper() + "\n");
                break;

            case "T3":
                Console.Write(args[2] + '\n');
                List<Node> solutionPath = SearchAlgorithm(level, state, param.Depth, param.Strategy);
                foreach (var nodeSolution in solutionPath) {
                    nodeSolution.PrintNode();
                }
                break;

            default:
                Console.WriteLine("Unknown action: " + param.Task);
                break;
        }

        // Parse first flag (level flag)
        switch (param.FirstFlag) {
            case "-l":
            case "-level":
                // Check if level argument is passed
                if (args.Length < 3) {
                    Console.WriteLine("-l usage: ./sokoban.exe <action> <parameters> '<level>'");
                    break;
                }
                //Console.WriteLine(level);   // print level
                break;

            default:
                Console.WriteLine("Unknown parameter: " + param.FirstFlag);
                break;
        }

        // Parse second flag (optional)
        if (param.SecondFlag != null) {
            switch (param.SecondFlag) {
                case "-s":
                    // Handle -s flag
                    break;
                case "-r":
                    renderActive = true;
                    break;

                default:
                    Console.WriteLine("Unknown parameter: " + param.SecondFlag);
                    break;
            }
        }

        // Parse strategy (optional)
        if (param.Strategy != null) {
            switch (param.Strategy) {
                case "BFS":
                    // Handle BFS strategy
                    break;

                case "DFS":
                    // Handle DFS strategy
                    break;

                case "UC":
                    // Handle UC strategy
                    break;

                case "GREEDY":
                    break;

                case "A*":
                    break;

                default:
                    Console.WriteLine("Unknown parameter: " + param.Strategy);
                    break;
            }
        }

        // Parse third flag (optional)
        if (param.ThirdFlag != null) {
            switch (param.ThirdFlag) {
                case "-d":
                    // Handle -d flag
                    break;

                default:
                    Console.WriteLine("Unknown parameter: " + param.ThirdFlag);
                    break;
            }
        }

        // Parse render flag (optional)
        if (param.RenderFlag != null) {
            switch (param.RenderFlag) {
                case "-r":
                    renderActive = true;
                    break;

                default:
                    Console.WriteLine("Unknown parameter: " + param.ThirdFlag);
                    break;
            }
        }


        // Render level graphics
        if (renderActive && !string.IsNullOrEmpty(param.Task) && !string.IsNullOrEmpty(param.Level)) {
            Renderer renderer = new Renderer(level, state);
            renderer.Render();
        }
    } // End Main

    /*********************************************************************
    * Method name: ProblemDomain
    *
    * Description of the Method: prints ID, rows, columns, walls, targets, 
    * player and boxes positions in the level.
    *
    * Calling arguments: Level level, State state
    *
    * Return value: void, does not return any values.
    *
    * Required Files: Does not make use of any external files
    *
    * List of Checked Exceptions and an indication of when each exception
    * is thrown: None
    *
    *********************************************************************/

    static void ProblemDomain(Level level, State state) {

        Console.WriteLine("\nID: " + state.Id);
        Console.WriteLine("\tRows: " + level.Rows);
        Console.WriteLine("\tColumns: " + level.Cols);
        Console.Write("\tWalls: ");
        PrintCoordsArray(level.Walls);
        Console.Write("\tTargets: ");
        PrintCoordsArray(level.Targets);
        Console.Write("\tPlayer: (" + state.Player.Item1 + "," + state.Player.Item2 + ")");
        Console.Write("\n\tBoxes: ");
        PrintCoordsArray(state.Boxes);
    }

    /*********************************************************************
      * Method name: SuccessorFunction
      *
      * Description of the Method: returns the list of successors, which are
      * all the possible moves that the player can make in a given state.
      *
      * Calling arguments: Level level, State state
      *
      * Return value: List of successors, each succesor is (char, string, int)
      *
      * Required Files: Does not make use of any external files
      *
      * List of Checked Exceptions and an indication of when each exception
      * is thrown: None
      *
      *********************************************************************/

    static List<Successor> SuccessorFunction(Level level, State state) {

        List<Successor> successors = new List<Successor>();

        (int, int) playerMove;
        (int, int) boxMove;
        int cost = 1;

        // Store direction coordinates
        var directions = new (int, int)[] { (-1, 0), (0, 1), (1, 0), (0, -1), };

        // Hashsets of walls and boxes array to check faster
        HashSet<(int, int)> wallsSet = new HashSet<(int, int)>();
        HashSet<(int, int)> boxesSet = new HashSet<(int, int)>();

        foreach (var wall in level.Walls) {
            wallsSet.Add((wall[0], wall[1]));
        }

        foreach (var box in state.Boxes) {
            boxesSet.Add((box[0], box[1]));
        }

        // Copy of boxes array
        int[][] newBoxes = new int[state.Boxes.Length][];
        for (int i = 0; i < state.Boxes.Length; i++) {
            newBoxes[i] = new int[state.Boxes[i].Length];
            Array.Copy(state.Boxes[i], newBoxes[i], state.Boxes[i].Length);
        }

        // Loop to check every direction (clock-wise)
        for (int i = 0; i < directions.Length; i++) {
            playerMove = (state.Player.Item1 + directions[i].Item1, state.Player.Item2 + directions[i].Item2);

            // Check wall collision
            if (!wallsSet.Contains(playerMove)) {

                State sucState = new State(playerMove, newBoxes);
                Successor successor = new Successor("NOTHING", sucState, 0);

                // Check box collision
                if (boxesSet.Contains(playerMove)) {
                    // Box collision

                    for (int j = 0; j < newBoxes.Length; j++) {
                        boxMove.Item1 = newBoxes[j][0] + directions[i].Item1;
                        boxMove.Item2 = newBoxes[j][1] + directions[i].Item2;

                        // Check if new box position is empty
                        if (!wallsSet.Contains(boxMove) && !boxesSet.Contains(boxMove)) {
                            // Find the box that is getting pushed by the player
                            if (newBoxes[j][0] == playerMove.Item1 && newBoxes[j][1] == playerMove.Item2) {

                                sucState.MovePlayer(newBoxes[j][0], newBoxes[j][1]);

                                newBoxes[j][0] += directions[i].Item1;
                                newBoxes[j][1] += directions[i].Item2;

                                // Sort coordinates
                                sucState.Boxes = newBoxes
                                        .OrderBy(box => box[0])
                                        .ThenBy(box => box[1])
                                        .Select(box => (int[])box.Clone()) // Deep copy each box
                                        .ToArray();

                                // Calculate new state id
                                sucState.Id = sucState.CalculateMD5Hash(sucState.Player, sucState.Boxes);

                                switch (i) {
                                    case 0:
                                        successor.Action = "U";
                                        successor.Cost = cost;
                                        successors.Add(successor);
                                        break;
                                    case 1:
                                        successor.Action = "R";
                                        successor.Cost = cost;
                                        successors.Add(successor);
                                        break;
                                    case 2:
                                        successor.Action = "D";
                                        successor.Cost = cost;
                                        successors.Add(successor);
                                        break;
                                    case 3:
                                        successor.Action = "L";
                                        successor.Cost = cost;
                                        successors.Add(successor);
                                        break;
                                }

                                // Revert boxes move to reset to original coords
                                newBoxes[j][0] -= directions[i].Item1;
                                newBoxes[j][1] -= directions[i].Item2;
                            }
                        }
                    }
                }
                else {
                    // No box collision
                    sucState.Id = sucState.CalculateMD5Hash(playerMove, newBoxes);

                    switch (i) {
                        case 0:
                            successor.Action = "u";
                            successor.Cost = cost;
                            successors.Add(successor);
                            break;
                        case 1:
                            successor.Action = "r";
                            successor.Cost = cost;
                            successors.Add(successor);
                            break;
                        case 2:
                            successor.Action = "d";
                            successor.Cost = cost;
                            successors.Add(successor);
                            break;
                        case 3:
                            successor.Action = "l";
                            successor.Cost = cost;
                            successors.Add(successor);
                            break;
                    }
                }
            }
        }

        return successors;
    }

    /*********************************************************************
      * Method name: ObjectiveFunction
      *
      * Description of the Method: returns if the objective function (all
      * boxes are on targets) is achieved.
      *
      * Calling arguments: Level level, State state
      *
      * Return value: boolean, returns true if the objective function is
      * achieved or false if not.
      *
      * Required Files: Does not make use of any external files
      *
      * List of Checked Exceptions and an indication of when each exception
      * is thrown: None
      *
      *********************************************************************/

    static bool ObjectiveFunction(Level level, State state) {

        HashSet<(int, int)> boxesSet = new HashSet<(int, int)>();
        HashSet<(int, int)> targetsSet = new HashSet<(int, int)>();


        foreach (var box in state.Boxes) {
            boxesSet.Add((box[0], box[1]));
        }

        foreach (var target in level.Targets) {
            targetsSet.Add((target[0], target[1]));
        }

        bool allBoxesOnTarget = boxesSet.IsSubsetOf(targetsSet);

        if (allBoxesOnTarget) {
            state.CalculateMD5Hash(state.Player, state.Boxes);
        }

        return allBoxesOnTarget;
    }

    /*********************************************************************
    * Method name: SearchAlgorithm
    *
    * Description of the Method: returns the list of nodes from the root
    * node to the objective node
    *
    * Calling arguments: Level level, State state, string depth, string
    * strategy
    *
    * Return value: int, returns the number of rows
    *
    * Required Files: Does not make use of any external files
    *
    * List of Checked Exceptions and an indication of when each exception
    * is thrown: None
    *
    *********************************************************************/

    static List<Node> SearchAlgorithm(Level level, State state, string depth, string strategy) {

        var comparator = Comparer<Node>.Create((x, y) => {
            int comparatorValue = x.ValueNode.CompareTo(y.ValueNode);
            return comparatorValue != 0 ? comparatorValue : x.IdNode.CompareTo(y.IdNode);
        });

        var frontier = new PriorityQueue<Node>(comparator);
        HashSet<string> visited = new HashSet<string>();
        bool solution = false;

        int maxDepth = int.Parse(depth);
        int totalNodes = 0;

        Node rootNode = new Node(totalNodes, state, null, "NOTHING", 0, 0.00f, 0.00f, 0.00f);
        rootNode.AssignValue(strategy, level);
        frontier.Add(rootNode); // insert root node in frontier

        Node node = rootNode;

        // Check if frontier is not empty and is not solution
        while (frontier.Count != 0 && !solution) {
            // is solution
            // extract node from frontier
            node = frontier.Poll();

            if (ObjectiveFunction(level, node.StateNode)) {
                //node state is objective
                solution = true;
            }
            else {
                if (node.Depth < maxDepth && !visited.Contains(node.StateNode.Id)) {
                    // correct depth and state not visited
                    visited.Add(node.StateNode.Id);

                    List<Successor> nodeSuccessors = SuccessorFunction(level, node.StateNode);
                    // expand node
                    foreach (var nodeSuc in nodeSuccessors) {
                        totalNodes++;
                        Node childNode = new Node(totalNodes, nodeSuc.State, node, nodeSuc.Action, node.Depth + 1, node.Cost + 1, 0.00f, 0.00f);
                        childNode.AssignValue(strategy, level);
                        frontier.Add(childNode);
                    }
                }
            }
        }

        // Check if a solution was found
        if (solution) {
            List<Node> solutionPath = new List<Node>();

            // Traverse from the current node to the root
            while (node != null) {
                solutionPath.Add(node);
                node = node.ParentNode;
            }
            solutionPath.Reverse();

            return solutionPath;
        }

        else {
            Console.WriteLine("There is no solution.");
            return null;
        }
    }

    /*********************************************************************
    * Method name: PrintCoordsArray
    *
    * Description of the Method: Prints a jagged array for debugging 
    * purposes
    *
    * Calling arguments: int[][] array
    *
    * Return value: void, does not return any values.
    *
    * Required Files: Does not make use of any external files
    *
    * List of Checked Exceptions and an indication of when each exception
    * is thrown: None
    *
    *********************************************************************/

    static void PrintCoordsArray(int[][] array) {
        Console.Write("[");

        for (int i = 0; i < array.Length; i++) {
            Console.Write("(" + array[i][0] + "," + array[i][1] + ")");

            if (i < array.Length - 1) {
                Console.Write(",");
            }
        }

        Console.WriteLine("]");
    }
}