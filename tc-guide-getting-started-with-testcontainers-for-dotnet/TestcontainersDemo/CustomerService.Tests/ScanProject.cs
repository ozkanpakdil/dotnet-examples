using Microsoft.CodeAnalysis;
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
        string filePath =
            @"C:\Users\ozkan\projects\dotnet-examples\tc-guide-getting-started-with-testcontainers-for-dotnet\TestcontainersDemo\CustomerService\CustomerService.cs";
        string code = File.ReadAllText(filePath);

        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        var variableCollector = new VariableCollector();
        variableCollector.Visit(root);

        _testOutputHelper.WriteLine("\nVariable Assignments:");
        foreach (var assignment in variableCollector.Assignments)
        {
            if (assignment.Key.ToLower().Contains("command"))
                _testOutputHelper.WriteLine($"Variable: {assignment.Key}, Assigned Value: {assignment.Value}");
        }
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