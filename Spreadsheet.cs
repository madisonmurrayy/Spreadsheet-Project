// <copyright file="Spreadsheet.cs" company="UofU-CS3500">
// Copyright (c) 2024 UofU-CS3500. All rights reserved.
// </copyright>

// Written by Joe Zachary for CS 3500, September 2013
// Update by Profs Kopta and de St. Germain, Fall 2021, Fall 2024
// author Madison Murray
// date Sept 2024
//     - Updated return types
//     - Updated documentation
namespace CS3500.Spreadsheet;

using CS3500.Formula;
using CS3500.DependencyGraph;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.ComponentModel.Design;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.VisualBasic;
using System.Xml.Linq;

/// <summary>
///   <para>
///     Thrown to indicate that a change to a cell will cause a circular dependency.
///   </para>
/// </summary>
public class CircularException : Exception
{
}

/// <summary>
///   <para>
///     Thrown to indicate that a name parameter was invalid.
///   </para>
/// </summary>
public class InvalidNameException : Exception
{
}

/// <summary>
/// <para>
///   Thrown to indicate that a read or write attempt has failed with
///   an expected error message informing the user of what went wrong.
/// </para>
/// </summary>
public class SpreadsheetReadWriteException : Exception
{
    /// <summary>
    ///   <para>
    ///     Creates the exception with a message defining what went wrong.
    ///   </para>
    /// </summary>
    /// <param name="msg"> An informative message to the user. </param>
    public SpreadsheetReadWriteException(string msg)
    : base(msg)
    {
    }
}

/// <summary>
///   <para>
///     An Spreadsheet object represents the state of a simple spreadsheet.  A
///     spreadsheet represents an infinite number of named cells.
///   </para>
/// <para>
///     Valid Cell Names: A string is a valid cell name if and only if it is one or
///     more letters followed by one or more numbers, e.g., A5, BC27.
/// </para>
/// <para>
///    Cell names are case insensitive, so "x1" and "X1" are the same cell name.
///    Your code should normalize (uppercased) any stored name but accept either.
/// </para>
/// <para>
///     A spreadsheet represents a cell corresponding to every possible cell name.  (This
///     means that a spreadsheet contains an infinite number of cells.)  In addition to
///     a name, each cell has a contents and a value.  The distinction is important.
/// </para>
/// <para>
///     The <b>contents</b> of a cell can be (1) a string, (2) a double, or (3) a Formula.
///     If the contents of a cell is set to the empty string, the cell is considered empty.
/// </para>
/// <para>
///     By analogy, the contents of a cell in Excel is what is displayed on
///     the editing line when the cell is selected.
/// </para>
/// <para>
///     In a new spreadsheet, the contents of every cell is the empty string. Note:
///     this is by definition (it is IMPLIED, not stored).
/// </para>
/// <para>
///     The <b>value</b> of a cell can be (1) a string, (2) a double, or (3) a FormulaError.
///     (By analogy, the value of an Excel cell is what is displayed in that cell's position
///     in the grid.) We are not concerned with cell values yet, only with their contents,
///     but for context:
/// </para>
/// <list type="number">
///   <item>If a cell's contents is a string, its value is that string.</item>
///   <item>If a cell's contents is a double, its value is that double.</item>
///   <item>
///     <para>
///       If a cell's contents is a Formula, its value is either a double or a FormulaError,
///       as reported by the Evaluate method of the Formula class.  For this assignment,
///       you are not dealing with values yet.
///     </para>
///   </item>
/// </list>
/// <para>
///     Spreadsheets are never allowed to contain a combination of Formulas that establish
///     a circular dependency.  A circular dependency exists when a cell depends on itself,
///     either directly or indirectly.
///     For example, suppose that A1 contains B1*2, B1 contains C1*2, and C1 contains A1*2.
///     A1 depends on B1, which depends on C1, which depends on A1.  That's a circular
///     dependency.
/// </para>
/// </summary>
public class Spreadsheet
{
    //dictionary maps a cell name to a cell object
    [JsonInclude]
    private Dictionary<string, Cell> Cells;

    //dependency graph holds and updates all dependencies between cell names
    private DependencyGraph CellGraph;

    //private variable holds non empty cells to be returned by GetNamesOfNonemptyCells method
    private HashSet<string> NonemptyCellNames;

    //private dictionary maps a cell name to its' value
    private Dictionary<string, object> ValueMap;

    //keeps the value of the Changed property
    private bool ChangedStatus = false;

    /// <summary>
    /// Constructs a spreadsheet using no previously saved data and initializes all member variable
    /// </summary>
    public Spreadsheet()
    {
        Cells = new Dictionary<string, Cell>();
        CellGraph = new DependencyGraph();
        NonemptyCellNames = new HashSet<string>();
        ValueMap = new Dictionary<string, object>();
    }

    /// <summary>
    /// Constructs a spreadsheet using the saved data in the file refered to by
    /// the given filename. 
    /// <see cref="Save(string)"/>
    /// </summary>
    /// <exception cref="SpreadsheetReadWriteException">
    ///   Thrown if the file can not be loaded into a spreadsheet for any reason
    /// </exception>
    /// <param name="filename">The path to the file containing the spreadsheet to load</param>
    public Spreadsheet(string filename) : this()
    {
        try
        {
            //read in the file to a string
            string CellsInJSON = File.ReadAllText(filename);
            //deserialize this JSON string
            Spreadsheet? tempSpreadsheet = JsonSerializer.Deserialize<Spreadsheet>(CellsInJSON);
            //mandatory null checking
            if (tempSpreadsheet != null)
            {
                //loop through each key value pair(kvp) in this deserialized dictionary
                foreach (var kvp in tempSpreadsheet.Cells)
                {
                    //set current cell 
                    Cell currentCell = kvp.Value;
                    //assure it's stringform isnt null
                    if (currentCell.StringForm != null)
                    {
                        //add cell to this dictionary
                        //CONTENT IS PASSED IN THROUGH STRING FORM
                        SetContentsOfCell(kvp.Key, currentCell.StringForm);
                    }
                }
            }
        }
        catch (Exception e)
        {
            throw new SpreadsheetReadWriteException(e.Message);
        }
    }

    ///
    /// 
    public string? GetCellStringForm(string cellName)
    {
        if (Cells.TryGetValue(cellName, out var outCell))
        {
            return outCell.StringForm;
        }
        else
        {
            //cell doesnt exist/have any stringForm
            return "";
        }
    }
    public string GetStringRepresentation()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            WriteIndented = true
        };
        //serialize this spreadsheet
        string SpreadsheetJSON = JsonSerializer.Serialize(this, options);
        return SpreadsheetJSON;
    }

    public void Update(string JSONRepresentation)
    {
        //this should reset everything
        Cells = new Dictionary<string, Cell>();
        CellGraph = new DependencyGraph();
        NonemptyCellNames = new HashSet<string>();
        ValueMap = new Dictionary<string, object>();

        Spreadsheet? tempSpreadsheet = JsonSerializer.Deserialize<Spreadsheet>(JSONRepresentation);

        //mandatory null checking
        if (tempSpreadsheet != null)
        {
            //loop through each key value pair(kvp) in this deserialized dictionary
            foreach (var kvp in tempSpreadsheet.Cells)
            {
                //set current cell 
                Cell currentCell = kvp.Value;
                //assure it's stringform isnt null
                if (currentCell.StringForm != null)
                {
                    //add cell to this dictionary
                    //CONTENT IS PASSED IN THROUGH STRING FORM
                    SetContentsOfCell(kvp.Key, currentCell.StringForm);
                }
            }
        }
    }
    //
    /// <summary>
    /// <para>
    /// Writes the contents of this spreadsheet to the named file using a JSON format.
    /// If the file already exists, overwrite it.
    /// </para>
    /// <para>
    /// The output JSON should look like the following.
    /// </para>
    /// <para>
    /// For example, consider a spreadsheet that contains a cell "A1"
    /// with contents being the double 5.0, and a cell "B3" with contents
    /// being the Formula("A1+2"), and a cell "C4" with the contents "hello".
    /// </para>
    /// <para>
    /// This method would produce the following JSON string:
    /// </para>
    /// <code>
    /// {
    /// "Cells": {
    /// "A1": {
    /// "StringForm": "5"
    /// },
    /// "B3": {
    /// "StringForm": "=A1+2"
    /// },
    /// "C4": {
    /// "StringForm": "hello"
    /// }
    /// }
    /// }
    /// </code>
    /// <para>
    /// You can achieve this by making sure your data structure is a dictionary
    /// and that the contained objects (Cells) have property named "StringForm"
    /// (if this name does not match your existing code, use the JsonPropertyName
    /// attribute).
    /// </para>
    /// <para>
    /// There can be 0 cells in the dictionary, resulting in { "Cells" : {} }
    /// </para>
    /// <para>
    /// Further, when writing the value of each cell...
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// If the contents is a string, the value of StringForm is that string
    /// </item>
    /// <item>
    /// If the contents is a double d, the value of StringForm isd.ToString()
    /// </item>
    /// <item>
    /// If the contents is a Formula f, the value of StringForm is "=" +f.ToString()
    /// </item>
    /// </list>
    /// </summary>
    /// <param name="filename"> The name (with path) of the file to saveto.</param>
    /// <exception cref="SpreadsheetReadWriteException">
    /// If there are any problems opening, writing, or closing the file,
    /// the method should throw a SpreadsheetReadWriteException with an
    /// explanatory message.
    /// </exception>
    public void Save(string filename)
    {
        //var options = new JsonSerializerOptions
        //{
        //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

        //    WriteIndented = true
        //};
        ////serialize this spreadsheet
        //string SpreadsheetJSON = JsonSerializer.Serialize(this, options);
        string SpreadsheetJSON = this.GetStringRepresentation();
        try
        {
            File.WriteAllText(filename, SpreadsheetJSON);
        }
        catch (Exception e)
        {
            throw new SpreadsheetReadWriteException(e.Message);
        }
        //after saving, the spreadsheet has not been changed
        ChangedStatus = false;
    }


    /// <summary>
    ///   <para>
    ///     Return the value of the named cell, as defined by
    ///     <see cref="GetCellValue(string)"/>.
    ///   </para>
    /// </summary>
    /// <param name="name"> The cell in question. </param>
    /// <returns>
    ///   <see cref="GetCellValue(string)"/>
    /// </returns>
    /// <exception cref="InvalidNameException">
    ///   If the provided name is invalid, throws an InvalidNameException.
    /// </exception>
    public object this[string name] => GetCellValue(name);


    /// <summary>
    /// True if this spreadsheet has been changed since it was 
    /// created or saved (whichever happened most recently),
    /// False otherwise.
    /// </summary>
    [JsonIgnore]
    public bool Changed
    {
        get
        {
            return ChangedStatus;
        }
    }

    /// <summary>
    ///   <para>
    ///     Return the value of the named cell.
    ///   </para>
    /// </summary>
    /// <param name="name"> The cell in question. </param>
    /// <returns>
    ///   Returns the value (as opposed to the contents) of the named cell.  The return
    ///   value should be either a string, a double, or a CS3500.Formula.FormulaError.
    /// </returns>
    /// <exception cref="InvalidNameException">
    ///   If the provided name is invalid, throws an InvalidNameException.
    /// </exception>
    public object GetCellValue(string name)
    {
        if (ValidName(name))
        {
            if (ValueMap.TryGetValue(name.ToUpper(), out var CellValue))
            {
                return CellValue;
            }
            else
            {
                return "";
            }
        }
        else
        {
            throw new InvalidNameException();
        }
    }

    /// <summary>
    ///   <para>
    ///     Set the contents of the named cell to be the provided string
    ///     which will either represent (1) a string, (2) a number, or 
    ///     (3) a formula (based on the prepended '=' character).
    ///   </para>
    ///   <para>
    ///     Rules of parsing the input string:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <para>
    ///         If 'content' parses as a double, the contents of the named
    ///         cell becomes that double.
    ///       </para>
    ///     </item>
    ///     <item>
    ///         If the string does not begin with an '=', the contents of the 
    ///         named cell becomes 'content'.
    ///     </item>
    ///     <item>
    ///       <para>
    ///         If 'content' begins with the character '=', an attempt is made
    ///         to parse the remainder of content into a Formula f using the Formula
    ///         constructor.  There are then three possibilities:
    ///       </para>
    ///       <list type="number">
    ///         <item>
    ///           If the remainder of content cannot be parsed into a Formula, a 
    ///           CS3500.Formula.FormulaFormatException is thrown.
    ///         </item>l
    ///         <item>
    ///           Otherwise, if changing the contents of the named cell to be f
    ///           would cause a circular dependency, a CircularException is thrown,
    ///           and no change is made to the spreadsheet.
    ///         </item>
    ///         <item>
    ///           Otherwise, the contents of the named cell becomes f.
    ///         </item>
    ///       </list>
    ///     </item>
    ///   </list>
    /// </summary>
    /// <returns>
    ///   <para>
    ///     The method returns a list consisting of the name plus the names 
    ///     of all other cells whose value depends, directly or indirectly, 
    ///     on the named cell. The order of the list should be any order 
    ///     such that if cells are re-evaluated in that order, their dependencies 
    ///     are satisfied by the time they are evaluated.
    ///   </para>
    ///   <example>
    ///     For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    ///     list {A1, B1, C1} is returned.
    ///   </example>
    /// </returns>
    /// <exception cref="InvalidNameException">
    ///     If name is invalid, throws an InvalidNameException.
    /// </exception>
    /// <exception cref="CircularException">
    ///     If a formula would result in a circular dependency, throws CircularException.
    /// </exception>
    public IList<string> SetContentsOfCell(string name, string content)
    {
        ChangedStatus = true;

        if (ValidName(name))
        {
            //if content starts with an =
            if ((content.Length > 1 && content.Substring(0, 1) == ("=")) || (content.Length == 1 && content == "="))
            {
                try
                {

                    if (content == "=")
                    {
                        Formula contentFormula = new Formula("");
                    }
                    else
                    {
                        Formula contentFormula = new Formula(content.Substring(1, content.Length - 1));
                        //add this formula object as the contents of this cell 
                        return SetCellContents(name, contentFormula);
                    }

                }
                catch (Exception e)
                {
                    if (e is FormulaFormatException)
                    {
                        //act as if the = is in the string and not an operator
                    }
                    else if (e is CircularException)
                    {
                        //nothing happens to spreadsheet
                        throw new CircularException();
                    }
                }
            }
            else if (double.TryParse(content, out double result))
            {
                //content must be a double
                return SetCellContents(name, result);
            }
            //must be a string
            return SetCellContents(name, content);
        }
        throw new InvalidNameException();
    }


    /// <summary>
    /// private helper method to detect an invalid name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private bool ValidName(string name)
    {
        //Use same standards as IsVar from Formula
        string VariableRegExPattern = @"[a-zA-Z]+\d+";
        string standaloneVarPattern = $"^{VariableRegExPattern}$";
        return Regex.IsMatch(name, standaloneVarPattern);
    }

    /// <summary>
    ///   Provides a copy of the normalized names of all of the cells in the spreadsheet
    ///   that contain information (i.e., non-empty cells).
    /// </summary>
    /// <returns>
    ///   A set of the names of all the non-empty cells in the spreadsheet.
    /// </returns>
    public ISet<string> GetNamesOfAllNonemptyCells()
    {
        return NonemptyCellNames;
    }

    /// <summary>
    ///   Returns the contents (as opposed to the value) of the named cell.
    /// </summary>
    /// <exception cref="InvalidNameException">
    ///   Thrown if the name is invalid.
    /// </exception>
    /// <param name="name">The name of the spreadsheet cell to query. </param>
    /// <returns>
    ///   The contents as either a string, a double, or a Formula.
    /// </returns>
    public object GetCellContents(string name)
    {
        //first check name
        if (ValidName(name))
        {
            if (Cells.TryGetValue(name.ToUpper(), out var outCell))
            {
                return outCell.Contents;
            }
            else //this cell is not in the map, and thus: empty
            {
                return "";
            }
        }
        else
        {
            throw new InvalidNameException();

        }
    }

    /// <summary>
    ///  Set the contents of the named cell to the given number.
    /// </summary>
    ///
    /// <exception cref="InvalidNameException">
    ///   If the name is invalid, throw an InvalidNameException.
    /// </exception>
    ///
    /// <param name="name"> The name of the cell. </param>
    /// <param name="number"> The new contents of the cell. </param>
    /// <returns>
    ///   <para>
    ///     This method returns an ordered list consisting of the passed in name
    ///     followed by the names of all other cells whose value depends, directly
    ///     or indirectly, on the named cell.
    ///   </para>
    ///   <para>
    ///     The order must correspond to a valid dependency ordering for recomputing
    ///     all of the cells, i.e., if you re-evaluate each cells in the order of the list,
    ///     the overall spreadsheet will be correctly updated.
    ///   </para>
    ///   <para>
    ///     For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
    ///     list [A1, B1, C1] is returned, i.e., A1 was changed, so then A1 must be
    ///     evaluated, followed by B1, followed by C1.
    ///   </para>
    ///   <para>
    ///   also will update the member DependencyGraph with whoever this cell may be dependent on
    ///   will update nonemptycell name list
    ///   
    /// </returns>
    private IList<string> SetCellContents(string name, double number)
    {
        if (ValidName(name.ToUpper()))
        {
            return SetCellHelper(name, number);

        }
        else
        {
            throw new InvalidNameException();
        }
    }

    /// <summary>
    ///   The contents of the named cell becomes the given text.
    /// </summary>
    /// update local dependency graph when this method is called - no matter WHAT this cell should depend on nothing
    /// <exception cref="InvalidNameException">
    ///   If the name is invalid, throw an InvalidNameException.
    /// </exception>
    /// <param name="name"> The name of the cell. </param>
    /// <param name="text"> The new contents of the cell. </param>
    /// <returns>
    ///   The same list as defined in <see cref="SetCellContents(string, double)"/>.
    /// </returns>
    private IList<string> SetCellContents(string name, string text)
    {
        if (ValidName(name.ToUpper()))
        {
            return SetCellHelper(name, text);

        }
        else
        {
            throw new InvalidNameException();
        }
        
    }

    /// <summary>
    ///   Set the contents of the named cell to the given formula.
    /// </summary>
    /// <exception cref="InvalidNameException">
    ///   If the name is invalid, throw an InvalidNameException.
    /// </exception>
    /// <exception cref="CircularException">
    ///   <para>
    ///     If changing the contents of the named cell to be the formula would
    ///     cause a circular dependency, throw a CircularException, and no
    ///     change is made to the spreadsheet.
    ///   </para>
    /// </exception>
    /// <param name="name"> The name of the cell. </param>
    /// <param name="formula"> The new contents of the cell. </param>
    /// <returns>
    ///   The same list as defined in <see cref="SetCellContents(string, double)"/>.
    /// </returns>
    private IList<string> SetCellContents(string name, Formula formula)
    {
        if (ValidName(name.ToUpper()))
        {
            return SetCellHelper(name, formula);

        }
        else
        {
              throw new InvalidNameException(); 
        }
    }

    /// <summary>
    /// Private helper method with general parameters to perform the 
    /// same operations in all SetCell method variations
    /// CELL VALUES ARE UPDATED IN THIS METHOD
    /// </summary>
    /// <param name="name"></param>
    /// <param name="contents"></param>
    /// <returns></returns>
    private List<string> SetCellHelper(string name, object contents)
    {
        ChangedStatus = true;
        //if this cell name already exists, just change the contents
        if (Cells.TryGetValue(name.ToUpper(), out var outCell))
        {
            object oldContents = outCell.Contents;
            //make sure we arent doing something redundent
            if (((contents is double && outCell.Contents is double) && ((double)(outCell.Contents) == (double)contents)) || ((contents is string && outCell.Contents is string) && ((string)(outCell.Contents) == (string)contents)) || ((contents is Formula && outCell.Contents is Formula) && ((Formula)(outCell.Contents) == (Formula)contents)))
            {
                ChangedStatus = false;
            }

            outCell.Contents = contents;

            //if the cell is now empty(contains ""), remove it from the Map and NonEmptyCellNames
            if (!outCell.isNonEmpty())
            {
                Cells.Remove(name.ToUpper());
                NonemptyCellNames.Remove(name.ToUpper());
            }
            //update member dependency graph so that this cell depends on no one
            CellGraph.ReplaceDependees(name.ToUpper(), outCell.getDependees());

            //this try catch block handles circula dependencies
            try
            {
                List<string> CellsToEvaluate = GetCellsToRecalculate(name.ToUpper()).ToList();
                UpdateEvaluations(CellsToEvaluate);
                return CellsToEvaluate;
            }
            catch (CircularException)
            {

                //nothing should happen to spreadsheet, reset to old contents
                SetCellHelper(name, oldContents);
                throw new CircularException();
            }


        }
        //we have a nonexistant cell
        else
        {
            //add the cell to the map
            Cell newCell = new Cell(name.ToUpper(), contents);
            //if this new cell is just "", then we should not add to map or NonEmptyCellNames list
            if (newCell.isNonEmpty())
            {
                //the map will only be updated if its nonempty, only strings can be empty
                Cells.Add(name.ToUpper(), newCell);
                NonemptyCellNames.Add(name.ToUpper());
            }
            CellGraph.ReplaceDependees(name.ToUpper(), newCell.getDependees());
            //catch a circular dependency with potential cell contents
            try
            {
                List<string> CellsToEvaluate = GetCellsToRecalculate(name.ToUpper()).ToList();
                UpdateEvaluations(CellsToEvaluate);
                return CellsToEvaluate;
            }
            catch (CircularException)
            {
                //this new cell shouldn't be added!
                SetCellHelper(name, "");
                throw new CircularException();
            }
        }
    }

    /// <summary>
    /// This method is responsible for actively updating the ValueMap and changing cell values
    /// </summary>
    /// <param name="CellsToBeChanged"></param>
    private void UpdateEvaluations(List<string> CellsToBeChanged)
    {
        foreach (string CellName in CellsToBeChanged)
        {
            object CurrentCellContents;
            //if this cell exists in our cell map(which it always should)
            if (Cells.TryGetValue(CellName, out var cell))
            {
                //get the contents of the current cell that needs to be evaluated
                CurrentCellContents = cell.Contents; //either a double, string, or formula
                if (CurrentCellContents is double)
                {
                    //replace value if it exists
                    if (ValueMap.ContainsKey(CellName))
                    {
                        ValueMap[CellName] = (double)CurrentCellContents;
                    }
                    else
                    {
                        ValueMap.Add(CellName.ToUpper(), (double)CurrentCellContents);
                    }
                }
                else if (CurrentCellContents is string)
                {
                    //replace value if it exists
                    if (ValueMap.ContainsKey(CellName))
                    {
                        ValueMap[CellName] = (string)CurrentCellContents;
                    }
                    else
                    {
                        ValueMap.Add(CellName.ToUpper(), (string)CurrentCellContents);
                    }
                }
                else//must be a formula
                {
                    object EvaluatedValue;
                    //evaluate the formula
                    EvaluatedValue = ((Formula)CurrentCellContents).Evaluate(CellLookup);

                    if (ValueMap.ContainsKey(CellName))
                    {
                        ValueMap[CellName] = EvaluatedValue;
                    }
                    else
                    {
                        ValueMap.Add(CellName, EvaluatedValue);
                    }
                }
            }//this is an empty cell, update value map with empty string
            else
            {
                if (ValueMap.ContainsKey(CellName))
                {
                    ValueMap[CellName] = "";
                }
                else
                {
                    ValueMap.Add(CellName.ToUpper(), "");
                }
            }
        }
    }

    /// <summary>
    /// This follows a lookup delegate criteria, and will return the cell value that is being looked up
    /// </summary>
    /// <param name="CellName"></param> cell to find value of
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception> no valid value at this cell
    private double CellLookup(string CellName)
    {
        //try to convert this cell's value to a double
        object CellValue = GetCellValue(CellName);
        if (CellValue is double)
        {
            return (double)CellValue;
        }
        else
        { //either not found or is a Formula Error
            throw new ArgumentException("Cell doesnt lead to any valid value");
        }
    }

    /// <summary>
    ///   Returns an enumeration, without duplicates, of the names of all cells whose
    ///   values depend directly on the value of the named cell.
    /// </summary>
    /// <param name="name"> This <b>MUST</b> be a valid name.  </param>
    /// <returns>
    ///   <para>
    ///     Returns an enumeration, without duplicates, of the names of all cells
    ///     that contain formulas containing name.
    ///   </para>
    ///   <para>For example, suppose that: </para>
    ///   <list type="bullet">
    ///      <item>A1 contains 3</item>
    ///      <item>B1 contains the formula A1 * A1</item>
    ///      <item>C1 contains the formula B1 + A1</item>
    ///      <item>D1 contains the formula B1 - C1</item>
    ///   </list>
    ///   <para> The direct dependents of A1 are B1 and C1. </para>
    /// </returns>
    /// //NOTE: CHANGE BACK TO PRIVATE
    public IEnumerable<string> GetDirectDependents(string name)
    {
        //doesnt matter if this cell has been initialized in the graph, if the cell is empty, things can still depend on it
        return CellGraph.GetDependents(name.ToUpper());

    }

    /// <summary>
    ///   <para>
    ///     This method is implemented for you, but makes use of your GetDirectDependents.
    ///   </para>
    ///   <para>
    ///     Returns an enumeration of the names of all cells whose values must
    ///     be recalculated, assuming that the contents of the cell referred
    ///     to by name has changed.  The cell names are enumerated in an order
    ///     in which the calculations should be done.
    ///   </para>
    ///   <exception cref="CircularException">
    ///     If the cell referred to by name is involved in a circular dependency,
    ///     throws a CircularException.
    ///   </exception>
    ///   <para>
    ///     For example, suppose that:
    ///   </para>
    ///   <list type="number">
    ///     <item>
    ///       A1 contains 5
    ///     </item>
    ///     <item>
    ///       B1 contains the formula A1 + 2.
    ///     </item>
    ///     <item>
    ///       C1 contains the formula A1 + B1.
    ///     </item>
    ///     <item>
    ///       D1 contains the formula A1 * 7.
    ///     </item>
    ///     <item>
    ///       E1 contains 15
    ///     </item>
    ///   </list>
    ///   <para>
    ///     If A1 has changed, then A1, B1, C1, and D1 must be recalculated,
    ///     and they must be recalculated in an order which has A1 first, and B1 before C1
    ///     (there are multiple such valid orders).
    ///     The method will produce one of those enumerations.
    ///   </para>
    ///   <para>
    ///      PLEASE NOTE THAT THIS METHOD DEPENDS ON THE METHOD GetDirectDependents.
    ///      IT WON'T WORK UNTIL GetDirectDependents IS IMPLEMENTED CORRECTLY.
    ///   </para>
    /// </summary>
    /// <param name="name"> The name of the cell.  Requires that name be a valid cell name.</param>
    /// <returns>
    ///    Returns an enumeration of the names of all cells whose values must
    ///    be recalculated.
    /// </returns>
    private IEnumerable<string> GetCellsToRecalculate(string name)
    {
        LinkedList<string> changed = new();
        HashSet<string> visited = [];
        Visit(name, name, visited, changed);
        return changed;
    }

    /// <summary>
    /// Helper method for GetCellsToRecalculate - loops through all of name's direct
    /// dependents, and all of those depenedents' direct dependents through recursion
    /// </summary>
    /// <param name="start"></param> the orgin of dependency
    /// <param name="name"></param> the current cell we are exploring dependents of
    /// <param name="visited"></param> all of the cells we have explored the dependents of
    /// <param name="changed"></param> all of the cells that will have to be recalculated
    /// <exception cref="CircularException"></exception>
    private void Visit(string start, string name, ISet<string> visited, LinkedList<string> changed)
    {
        //keep track of every cell whose dependents we have visited, starting with the "name" cell
        visited.Add(name);
        //loop through all cells that must be recalculated after this cell is updated
        foreach (string n in GetDirectDependents(name))
        {
            //if we find that this cell depends on itself, throw circular exception
            if (n.Equals(start))
            {
                throw new CircularException();
            }
            //if we havent visited this dependent yet, re-call visit with this dependent as the current "name"
            else if (!visited.Contains(n))
            {
                //recursive call to this cell and all of its dependents
                Visit(start, n, visited, changed);
            }
        }
        //after we have visited all direct dependents of this cell, add it to the list to be returned
        changed.AddFirst(name);
    }

    /// <summary>
    /// Nested private Cell class to hold Cell objects and their properties:
    ///     contents, name, empty/full status, dependees
    /// </summary>

    private class Cell
    {
        [JsonInclude]
        public string? StringForm { get; set; } //holds the string form of the contents
        private object contents; //holds the 1)double, 2)string, or 3)Formula object that appears on the spreadsheet edit bar
        private string name; //cell name
                             // private double value; //TO BE IMPLEMENTED - holds the, respective, 1)double value, 2)string value,
                             //or 3)double or FormulaError(Evaluate output) value that the cell shows on the spreadsheet
        private bool empty = false; //an "initialized" cell may be holding an empty string, so it shouldnt be added to the spreadsheet's
                                    //dictionary or the list of NonEmptyCells
        private HashSet<string> dependees; //string set of names that appear in this cell's formula:IE cells this cell DEPENDS ON (its dependees)

        /// <summary>
        /// No parameter constructor used for json
        /// </summary>
        public Cell()
        {
            contents = "";
            name = "";
            dependees = new HashSet<string>();
            //StringForm = "";

        }
        /// <summary>
        /// creates a new cell object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contents"></param>
        public Cell(string name, object contents)
        {
            StringForm = Convert.ToString(contents);
            this.name = name;
            this.contents = contents;
            dependees = new HashSet<string>();
            //the set contents method makes sure to update all member variables and properties of this Cell
            setContents(contents);
        }

        /// <summary>
        /// simple public getter method for the current state of this cell's contents
        /// returns false if and only if the contents is and empty string("")
        /// </summary>
        /// <returns></returns>
        public bool isNonEmpty()
        {
            if (this.empty == false)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// this method assures that ALL cell properties are updated when the contents are updated
        /// </summary>
        /// <param name="contents"></param>
        public void setContents(object contents)
        {
            this.empty = false;
            if (contents is string) //handle all string contents
            {
                StringForm = (string)contents;
                //special case
                if (contents.Equals(""))
                {

                    this.empty = true;
                }
                this.contents = contents;
                //this cell depends on nothing if a cell value is a string
                dependees = new HashSet<string>();

            }
            else if (contents is Formula) //handle all Formula contents
            {
                //add the equals sign to the string form of a Formula
                StringForm = "=" + ((Formula)(contents)).ToString();
                //set member variable
                this.contents = contents;
                //reset dependees map incase it was holding previous dependees
                dependees = new HashSet<string>();
                //cast so I can call methods on a Formula object
                Formula f1 = (Formula)contents;
                //update dependents based on the formulas contents(variables)
                HashSet<string> variablesInFormula = (HashSet<string>)f1.GetVariables();
                //this cell depends on each variable that appears in its formula
                foreach (string varName in variablesInFormula)
                {
                    dependees.Add(varName);
                }
            }
            else //must be a double
            {
                StringForm = contents.ToString();
                this.contents = contents;
                //there are no dependees for a double(it depends on nothing)
                dependees = new HashSet<string>();
                //set value with evaluate method
            }
        }

        /// <summary>
        /// getter method to be used to add dependees of THIS CELL(what depends on this cell) to the dependency graph in the Spreadsheet class
        /// updated when the strings contents are updated
        /// </summary>
        /// <returns></returns>
        public HashSet<string> getDependees()
        {
            return dependees;
        }

        [JsonIgnore]
        /// <summary>
        /// Contents property for easy getting and setting through the Spreadsheet class
        /// </summary>
        public object Contents
        {
            get
            {
                return contents;
            }
            set
            {
                setContents(value);
            }


        }
    }
}
