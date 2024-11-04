// <copyright file="Formula_PS2.cs" company="UofU-CS3500">
// Copyright (c) 2024 UofU-CS3500. All rights reserved.
// </copyright>
// <summary>
//   <para>
//     This code is provides to start your assignment.  It was written
//     by Profs Joe, Danny, and Jim.  
//     @author Madison Murray
//     @date Sep 2024
//   </para>
// </summary>
namespace CS3500.Formula;

using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
///   <para>
///     This class represents formulas written in standard infix notation using standard precedence
///     rules.  The allowed symbols are non-negative numbers written using double-precision
///     floating-point syntax; variables that consist of one ore more letters followed by
///     one or more numbers; parentheses; and the four operator symbols +, -, *, and /.
///   </para>
///   <para>
///     Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
///     a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable;
///     and "x 23" consists of a variable "x" and a number "23".  Otherwise, spaces are to be removed.
///   </para>
/// </summary>
public class Formula
{
    ///private member variables are all listed below
    ///allTokens holds the list returned from the getTokens mfethod- valid and invalid tokens
    private List<string> allTokens = new List<string>();

    ///allValidTokens holds all of the valid tokens found as they are found in the isValid method
    private List<string> allValidTokens = new List<String>();

    //allValidTokensString holds all of the valid tokens in the form to be returned by the toString method
    private string allValidTokensString = "";

    //holds the evaluated value of the formula
    private double evaluatedValue;

    /// <summary>
    ///   All variables are letters followed by numbers.  This pattern
    ///   represents valid variable name strings.
    /// </summary>
    private const string VariableRegExPattern = @"[a-zA-Z]+\d+";

    /// <summary>
    ///   Initializes a new instance of the <see cref="Formula"/> class.
    ///   <para>
    ///     Creates a Formula from a string that consists of an infix expression written as
    ///     described in the class comment.  If the expression is syntactically incorrect,
    ///     throws a FormulaFormatException with an explanatory Message.  
    ///   </para>
    ///   <para>
    ///     Non Exhaustive Example Errors:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///        Invalid variable name, e.g., x, x1x  (Note: x1 is valid, but would be normalized to X1)
    ///     </item>
    ///     <item>
    ///        Empty formula, e.g., string.Empty
    ///     </item>
    ///     <item>
    ///        Mismatched Parentheses, e.g., "(("
    ///     </item>
    ///     <item>
    ///        Invalid Following Rule, e.g., "2x+5"
    ///     </item>
    ///   </list>
    /// </summary>
    /// <param name="formula"> The string representation of the formula to be created.</param>
    public Formula(string formula)
    {
        allTokens = GetTokens(formula);
        IsValid(allTokens);
    }

    /// <summary>
    ///   <para>
    ///     Reports whether f1 == f2, using the notion of equality from the <see cref="Equals"/> method.
    ///   </para>
    /// </summary>
    /// <param name="f1"> The first of two formula objects. </param>
    /// <param name="f2"> The second of two formula objects. </param>
    /// <returns> true if the two formulas are the same.</returns>
    public static bool operator ==(Formula f1, Formula f2)
    {
        if (f1.Equals(f2)) return true;
        else return false;
    }

    /// <summary>
    ///   <para>
    ///     Reports whether f1 != f2, using the notion of equality from the <see cref="Equals"/> method.
    ///   </para>
    /// </summary>
    /// <param name="f1"> The first of two formula objects. </param>
    /// <param name="f2"> The second of two formula objects. </param>
    /// <returns> true if the two formulas are not equal to each other.</returns>
    public static bool operator !=(Formula f1, Formula f2)
    {
        if (f1.Equals(f2)) return false;
        else return true;
    }

    /// <summary>
    ///   <para>
    ///     Determines if two formula objects represent the same formula.
    ///   </para>
    ///   <para>
    ///     By definition, if the parameter is null or does not reference 
    ///     a Formula Object then return false.
    ///   </para>
    ///   <para>
    ///     Two Formulas are considered equal if their canonical string representations
    ///     (as defined by ToString) are equal.  
    ///   </para>
    /// </summary>
    /// <param name="obj"> The other object.</param>
    /// <returns>
    ///   True if the two objects represent the same formula.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (!(obj is Formula))
        {
            return false;
        }
        else
        {
            return this.ToString().Equals(obj.ToString());
        }
    }

    /// <summary>
    ///   <para>
    ///     Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
    ///     case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two
    ///     randomly-generated unequal Formulas have the same hash code should be extremely small.
    ///   </para>
    /// </summary>
    /// <returns> The hashcode for the object. </returns>
    public override int GetHashCode()
    {
        return this.ToString().GetHashCode();
    }


    /// <summary>
    /// <para>
    ///     Private helper method returns true or false depending on if the token passes 
    /// </para>
    ///  through is a number
    /// </summary>
    /// <param name="value"> The token string value to be detirmined a number or not. </param>
    /// <returns></returns>
    private bool isNumber(string value)
    {
        int param = 0;
        if (int.TryParse(value, out param))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// </para>
    ///     Sorts through all tokens found in the original expression and identifies
    ///     them as an open/closed parenthesis, operator, variable, number, or an invalid token. According to these
    ///     identifications the token is compared to its position and the previous token to see if it passes
    ///     or fails any of the 8 rules presented in assignment documentation.
    /// <para>
    /// </summary>
    /// <param name="tokens"></param>
    /// <exception cref="FormulaFormatException"></exception>
    private void IsValid(List<string> tokens)
    {
        //immedietly throw if the formula is empty
        if (tokens.Count == 0)
        {
            throw new FormulaFormatException("The formula is empty");
        }

        int openParenCount = 0;
        int closedParenCount = 0;
        int indexCount = 0;
        string prevToken = "";
        string currentToken = "";
        double parsingVariable;

        foreach (string token in tokens)
        {
            currentToken = token;
            indexCount++;
            if (IsVar(token))
            {
                //RULE 8: if previous token is a (, var, or number, current cannot be any of those.
                if ((indexCount > 1) && (prevToken == ")" || IsVar(prevToken) || isNumber(prevToken)))
                {
                    throw new FormulaFormatException("Rule 8 violated: variable follows invalid token");
                }

                //add this token to the toString return string (in all uppercase)
                allValidTokensString += token.ToUpper();
            }
            else if (token == "(")
            {
                openParenCount++;

                //RULE 6: last token rule
                if (indexCount == tokens.Count)
                {
                    //last token cannot be an open paren
                    throw new FormulaFormatException("The last token cannot be an open parenthesis");

                }

                //RULE 8: if previous token is a (, var, or number, current cannot be any of those.
                if ((indexCount > 1) && (prevToken == ")" || IsVar(prevToken) || isNumber(prevToken)))
                {
                    throw new FormulaFormatException("Rule 8 violated: open paren follows invalid token");
                }

                //add this token to the toString return string
                allValidTokensString += token;
            }
            else if (token == ")")
            {
                closedParenCount++;

                //RULE 5: first token rule
                if (indexCount == 1)
                {
                    //first token cannot be a closed paren
                    throw new FormulaFormatException("The first token cannot be a closed parenthesis");
                }

                //RULE 3: closing parenthesis rule
                if (closedParenCount > openParenCount)
                {
                    throw new FormulaFormatException("Rule 3 violated: too many closed parenthesis before open");
                }

                //RULE 7: if previous token is operator or open paren, current cannot be close paren or operator
                if ((indexCount > 1) && (prevToken == "+" || prevToken == "*" || prevToken == "/" || prevToken == "-" || prevToken == "("))
                {
                    throw new FormulaFormatException("Rule 7 is violated: closed parenthesis follows invalid token");
                }

                //add this token to the toString return string
                allValidTokensString += token;
            }
            else if (token == "*" || token == "+" || token == "-" || token == "/")
            {
                //RULE 5: first token rule
                if (indexCount == 1)
                {
                    //first token cannot be a closed paren
                    throw new FormulaFormatException("The first token cannot be an operator");
                }

                //RULE 6: last token rule
                if (indexCount == tokens.Count)
                {
                    //last token cannot be an operator
                    throw new FormulaFormatException("The last token cannot be an operator");
                }

                //RULE 7: if previous token is operator or open paren, current cannot be close paren or operator
                if ((indexCount > 1) && (prevToken == "+" || prevToken == "*" || prevToken == "/" || prevToken == "-" || prevToken == "("))
                {
                    throw new FormulaFormatException("Rule 7 is violated: operator follows invalid token");
                }

                //add this token ot the toString return string
                allValidTokensString += token;
            }
            else if (double.TryParse(token, out parsingVariable))
            {
                //RULE 8: if previous token is a (, var, or number, current cannot be any of those.
                if ((indexCount > 1) && (prevToken == ")" || IsVar(prevToken) || isNumber(prevToken)))
                {
                    throw new FormulaFormatException("Rule 8 violated: operator follows invalid token");

                }

                //add the parsed double value to the toString return string
                allValidTokensString += parsingVariable.ToString();
            }
            else
            {
                throw new FormulaFormatException("No valid tokens");
            }

            //allValidTokens includes all upercase variables and scientific notation UNSIMPLIFIED
            allValidTokens.Add(token.ToUpper());

            //reset previous token
            prevToken = token;

        }

        //RULE 1: there must be at least one valid token
        if (allValidTokens.Count == 0)
        {
            throw new FormulaFormatException("Rule 1 violated: no valid tokens in expression");
        }

        //RULE 4: closed and open parenthesis count must match
        if (closedParenCount != openParenCount)
        {
            throw new FormulaFormatException("Rule 4 violated: uneven count of open and closed parenthesis");
        }
    }

    /// <summary>
    ///   <para>
    ///     Returns a set of all the variables in the formula.
    ///   </para>
    ///   <remarks>
    ///     Important: no variable may appear more than once in the returned set, even
    ///     if it is used more than once in the Formula.
	///     Variables should be returned in canonical form, having all letters converted
	///     to uppercase.
    ///   </remarks>
    ///   <list type="bullet">
    ///     <item>new("x1+y1*z1").GetVariables() should return a set containing "X1", "Y1", and "Z1".</item>
    ///     <item>new("x1+X1"   ).GetVariables() should return a set containing "X1".</item>
    ///   </list>
    /// </summary>
    /// <returns> the set of variables (string names) representing the variables referenced by the formula. </returns>
    public ISet<string> GetVariables()
    {
        HashSet<string> variables = new HashSet<string>();
        foreach (var token in allValidTokens)
        {
            if (IsVar(token))
            {
                variables.Add(token);
            }
        }
        return variables;
    }

    /// <summary>
    ///   <para>
    ///     Evaluates this Formula, using the lookup delegate to determine the values of
    ///     variables.
    ///   </para>
    ///   <remarks>
    ///     When the lookup method is called, it will always be passed a normalized (capitalized)
    ///     variable name.  The lookup method will throw an ArgumentException if there is
    ///     not a definition for that variable token.
    ///   </remarks>
    ///   <para>
    ///     If no undefined variables or divisions by zero are encountered when evaluating
    ///     this Formula, the numeric value of the formula is returned.  Otherwise, a 
    ///     FormulaError is returned (with a meaningful explanation as the Reason property).
    ///   </para>
    ///   <para>
    ///     This method should never throw an exception.
    ///   </para>
    /// </summary>
    /// <param name="lookup">
    ///   <para>
    ///     Given a variable symbol as its parameter, lookup returns the variable's value
    ///     (if it has one) or throws an ArgumentException (otherwise).  This method will expect 
    ///     variable names to be normalized.
    ///   </para>
    /// </param>
    /// <returns> Either a double or a FormulaError, based on evaluating the formula.</returns>
    public object Evaluate(Lookup lookup)
    {
        //lookup takes in a string and returns a double, always

        Stack<double> valueStack = new Stack<double>();
        Stack<string> operatorStack = new Stack<string>();

        int indexCount = 0; //keep track of when we are done iterating through all valid tokens

        //passed from isValid, so allValidTokens is initialized and valid
        foreach (string token in this.allValidTokens)
        {
            string currentToken = token;
            //begining of variable or number tokens
            if (IsVar(token) || double.TryParse(token, out var parsingVariable))
            {
                //if this token is a variable
                //if * or / is at the top of operator stack
                if (IsOnTop("*", operatorStack))
                {
                    try
                    {
                        evaluateNumber("*", valueStack, token, lookup);
                    }
                    catch (ArgumentException e) //account for if the lookup has no variable value
                    {
                        return new FormulaError(e.Message);
                    }
                    operatorStack.Pop(); // remove the * after evaluating
                }
                else if (IsOnTop("/", operatorStack))
                {
                    try
                    {
                        evaluateNumber("/", valueStack, token, lookup);
                    }
                    catch (Exception e)
                    {
                        return new FormulaError(e.Message);
                    }

                    operatorStack.Pop(); //remove the / after evaluating
                }
                else // there may be another operator, just not a * or /
                {
                    if (IsVar(token))
                    {
                        try
                        {
                            valueStack.Push(lookup(currentToken));
                        }
                        catch (Exception e)
                        {
                            return new FormulaError(e.Message);
                        }
                    }
                    else
                    {
                        valueStack.Push(double.Parse(token));
                    }
                }
            } //end of variable and number handling
            //HANDLE (
            else if (token == "(")
            {
                operatorStack.Push(token);
            }
            //HANDLE  )
            else if (token == ")")
            {
                //if + or - is at the top of operator stack
                if (IsOnTop("+", operatorStack))
                {
                    operatorStack.Pop();
                    UseOperator("+", valueStack, indexCount);
                }
                else if (IsOnTop("-", operatorStack))
                {
                    operatorStack.Pop();
                    UseOperator("-", valueStack, indexCount);
                } //done with first peek

                //garunteed ( to pop
                operatorStack.Pop();

                //look for multiplication or division
                if (IsOnTop("*", operatorStack))
                {
                    operatorStack.Pop();
                    UseOperator("*", valueStack, indexCount);
                }
                else if (IsOnTop("/", operatorStack))
                {
                    try
                    {
                        operatorStack.Pop();
                        UseOperator("/", valueStack, indexCount);
                    }
                    catch (DivideByZeroException e)
                    {
                        return new FormulaError(e.Message);
                    }
                }

            }//end of closed parnthesis
            //HANDLE + OR -
            else if (token == "+" || token == "-")
            {
                if (IsOnTop("+", operatorStack))
                {
                    UseOperator("+", valueStack, indexCount);
                }
                else if (IsOnTop("-", operatorStack))
                {
                    UseOperator("-", valueStack, indexCount);
                }
                //push current value onto the operator stack no matter what
                operatorStack.Push(currentToken);
            }
            else if (token == "*" || token == "/")
            {
                operatorStack.Push(currentToken);

            }

            indexCount++;
        }
        //MADE IT THROUGH ALL TOKENS 
        if (operatorStack.Count == 0)
        {
            //if operator stack is empty, return result
            evaluatedValue = valueStack.Pop();
        }
        else
        {
            UseOperator(operatorStack.Pop(), valueStack, indexCount);
        }

        return evaluatedValue;
    }


    /// <summary>
    /// private helper method to evaluate operations between numbers and variables
    /// since they undergo the same procedures when detected in the stack
    /// </summary>
    /// <param name="operatorToUse"></param>
    /// <param name="stack"></param>
    /// <param name="currentToken"></param>
    /// <param name="lookup"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DivideByZeroException"></exception>
    private void evaluateNumber(string operatorToUse, Stack<double> stack, string currentToken, Lookup lookup)
    {
        double firstPopped = stack.Pop();//need to store one variable since we are popping two
        double currentTokenNumerical; //used to convert variable or string to a double 
        if (IsVar(currentToken))
        {
            try
            {
                currentTokenNumerical = lookup(currentToken);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Message);
            }
        }
        else
        {
            currentTokenNumerical = double.Parse(currentToken);
        }
        if (operatorToUse == "*")
        {
            stack.Push(currentTokenNumerical * firstPopped);
        }
        else if (operatorToUse == "/")
        {
            //check that we arent dividing by zero
            if (currentTokenNumerical == 0)
            {
                throw new DivideByZeroException("cannot divide by zero");
            }
            stack.Push(firstPopped / currentTokenNumerical);
        }
    }

    /// <summary>
    /// Private helper method to execute lines of code repeated often, this
    /// performs the operation with operatorToUse on the two most recent items on the stack
    /// NOTE: when doing /, check for dividing by zero
    /// </summary>
    /// <param name="operatorToUse"></param>
    /// <param name="stack"></param> the value stack
    private void UseOperator(string operatorToUse, Stack<double> stack, int indexCount)
    {
        double localEvaluatedValue;
        double firstPopped = stack.Pop();

        if (operatorToUse == "-")
        {
            localEvaluatedValue = (stack.Pop() - firstPopped);
        }
        else if (operatorToUse == "+")
        {
            localEvaluatedValue = (stack.Pop() + firstPopped);
        }
        else if (operatorToUse == "*")
        {
            localEvaluatedValue = (stack.Pop() * firstPopped);
        }
        else
        {
            if (firstPopped == 0)
            {
                throw new DivideByZeroException("Cannot divide by zero");
            }
            localEvaluatedValue = (stack.Pop() / firstPopped);
        }
        //push the evaluated result
        stack.Push(localEvaluatedValue);

        //check if this is the last evaluation and set the final variable
        if (indexCount == allValidTokens.Count)
        {
            evaluatedValue = localEvaluatedValue;
        }

    }

    /// <summary>
    /// Private helper method to detirmine if the operator we are looking for is
    /// at the top of the operator stack
    /// </summary>
    /// <param name="target"></param>
    /// <param name="stack"></param>
    /// <returns></returns>
    private bool IsOnTop(string target, Stack<string> stack)
    {
        if (stack.TryPeek(out var result))
        {
            if ((string)result == target)
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    ///   Any method meeting this type signature can be used for
    ///   looking up the value of a variable.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///   If a variable name is provided that is not recognized by the implementing method,
    ///   then the method should throw an ArgumentException.
    /// </exception>
    /// <param name="variableName">
    ///   The name of the variable (e.g., "A1") to lookup.
    /// </param>
    /// <returns> The value of the given variable (if one exists). </returns>
    public delegate double Lookup(string variableName);


    /// <summary>
    ///   <para>
    ///     Returns a string representation of a canonical form of the formula.
    ///   </para>
    ///   <para>
    ///     The string will contain no spaces.
    ///   </para>
    ///   <para>
    ///     If the string is passed to the Formula constructor, the new Formula f 
    ///     will be such that this.ToString() == f.ToString().
    ///   </para>
    ///   <para>
    ///     All of the variables in the string will be normalized.  This
    ///     means capital letters.
    ///   </para>
    ///   <para>
    ///       For example:
    ///   </para>
    ///   <code>
    ///       new("x1 + y1").ToString() should return "X1+Y1"
    ///       new("X1 + 5.0000").ToString() should return "X1+5".
    ///   </code>
    ///   <para>
    ///     This code should execute in O(1) time.
    ///   <para>
    /// </summary>
    /// <returns>
    ///   A canonical version (string) of the formula. All "equal" formulas
    ///   should have the same value here.
    /// </returns>
    public override string ToString()
    {
        return allValidTokensString;
    }

    /// <summary>
    ///   Reports whether "token" is a variable.  It must be one or more letters
    ///   followed by one or more numbers.
    /// </summary>
    /// <param name="token"> A token that may be a variable. </param>
    /// <returns> true if the string matches the requirements, e.g., A1 or a1. </returns>
    private static bool IsVar(string token)
    {
        // notice the use of ^ and $ to denote that the entire string being matched is just the variable
        string standaloneVarPattern = $"^{VariableRegExPattern}$";
        return Regex.IsMatch(token, standaloneVarPattern);
    }

    /// <summary>
    ///   <para>
    ///     Given an expression, enumerates the tokens that compose it.
    ///   </para>
    ///   <para>
    ///     Tokens returned are:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>left paren</item>
    ///     <item>right paren</item>
    ///     <item>one of the four operator symbols</item>
    ///     <item>a string consisting of one or more letters followed by one or more numbers</item>
    ///     <item>a double literal</item>
    ///     <item>and anything that doesn't match one of the above patterns</item>
    ///   </list>
    ///   <para>
    ///     There are no empty tokens; white space is ignored (except to separate other tokens).
    ///   </para>
    /// </summary>
    /// <param name="formula"> A string representing an infix formula such as 1*B1/3.0. </param>
    /// <returns> The ordered list of tokens in the formula. </returns>
    private static List<string> GetTokens(string formula)
    {
        List<string> results = [];

        string lpPattern = @"\(";
        string rpPattern = @"\)";
        string opPattern = @"[\+\-*/]";
        string doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
        string spacePattern = @"\s+";

        // Overall pattern
        string pattern = string.Format(
                                        "({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                        lpPattern,
                                        rpPattern,
                                        opPattern,
                                        VariableRegExPattern,
                                        doublePattern,
                                        spacePattern);

        // Enumerate matching tokens that don't consist solely of white space.
        foreach (string s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
        {
            if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
            {
                results.Add(s);
            }
        }

        return results;
    }
}


/// <summary>
///   Used to report syntax errors in the argument to the Formula constructor.
/// </summary>
public class FormulaFormatException : Exception
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="FormulaFormatException"/> class.
    ///   <para>
    ///      Constructs a FormulaFormatException containing the explanatory message.
    ///   </para>
    /// </summary>
    /// <param name="message"> A developer defined message describing why the exception occured.</param>
    public FormulaFormatException(string message)
        : base(message)
    {
        // All this does is call the base constructor. No extra code needed.
    }
}

/// <summary>
/// Used as a possible return value of the Formula.Evaluate method.
/// </summary>
public class FormulaError
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="FormulaError"/> class.
    ///   <para>
    ///     Constructs a FormulaError containing the explanatory reason.
    ///   </para>
    /// </summary>
    /// <param name="message"> Contains a message for why the error occurred.</param>
    public FormulaError(string message)
    {
        Reason = message;
    }

    /// <summary>
    ///  Gets the reason why this FormulaError was created.
    /// </summary>
    public string Reason { get; private set; }
}
