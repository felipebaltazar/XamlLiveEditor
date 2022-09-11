using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace XamlLiveCode.SourceGenerator
{
    internal sealed class SyntaxReceiver : ISyntaxReceiver
    {
        private const string XamlAttribute = "Xamarin.Forms.Xaml.XamlFilePathAttribute";
        private List<ClassDeclarationSyntax> _classes = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode context)
        {
            if (!(context is ClassDeclarationSyntax classDeclarationSyntax))
                return;

            _classes.Add(classDeclarationSyntax);
        }

        public List<ClassDeclarationSyntax> GetClasses(Func<SyntaxTree, SemanticModel> semanticModelGetter)
        {
            var classes = new List<ClassDeclarationSyntax>();
            foreach (var classDeclarationSyntax in _classes)
            {
                var semanticModel = semanticModelGetter?.Invoke(classDeclarationSyntax.SyntaxTree);
                if (semanticModel is null)
                    continue;

                if (!(semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol namedSymbol))
                    continue;

                var attributeData = classDeclarationSyntax
                    .AttributeLists
                    .SelectMany(x => x.Attributes)
                    .FirstOrDefault(ad => ad.Name.ToString().EndsWith(XamlAttribute, StringComparison.OrdinalIgnoreCase));

                if (attributeData is null)
                    continue;

                classes.Add(classDeclarationSyntax);
            }

            return classes;
        }
    }
}
