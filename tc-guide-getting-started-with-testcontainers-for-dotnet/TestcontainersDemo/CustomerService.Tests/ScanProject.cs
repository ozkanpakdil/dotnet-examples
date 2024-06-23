using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;

namespace CustomerService.Tests;

public sealed class VariableTracker
{
    private readonly ITestOutputHelper _testOutputHelper;

    public VariableTracker(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void ScanFile()
    {
        const string filePath = "../../../../CustomerService/CustomerService.cs";
        var code = File.ReadAllText(filePath);

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        var variableCollector = new VariableCollector();
        variableCollector.Visit(root);

        _testOutputHelper.WriteLine("\nVariable Assignments:");
        variableCollector.Assignments
            .Where(assignment => assignment.Key.ToLower().Contains("command"))
            .Select(assignment => $"Variable: {assignment.Key}, Assigned Value: {assignment.Value}")
            .ToList()
            .ForEach(_testOutputHelper.WriteLine);
    }

    private class VariableCollector : CSharpSyntaxWalker
    {
        public Dictionary<string, string> Assignments { get; } = new();

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = node.Left.ToString();
            var right = node.Right.ToString();
            Assignments[left] = right;
            base.VisitAssignmentExpression(node);
        }
    }
}