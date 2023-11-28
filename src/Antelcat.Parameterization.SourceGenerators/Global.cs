using System;

namespace Antelcat.Parameterization.SourceGenerators;

public static class Global
{
    public const string Namespace = $"{nameof(Antelcat)}.{nameof(Parameterization)}";
    public const string GlobalNamespace = $"global::{nameof(Antelcat)}.{nameof(Parameterization)}";
    
    public const string Boolean = $"global::{nameof(System)}.{nameof(System.Boolean)}";
    public const string String = $"global::{nameof(System)}.{nameof(System.String)}";
    public const string ValueTuple = $"global::{nameof(System)}.{nameof(System.ValueTuple)}";
    public const string GenericList = $"global::{nameof(System)}.{nameof(System.Collections)}.{nameof(System.Collections.Generic)}.List";
    public const string GenericIReadonlyList = $"global::{nameof(System)}.{nameof(System.Collections)}.{nameof(System.Collections.Generic)}.IReadOnlyList";
    
    public const string TypeConverter = $"global::{nameof(System)}.ComponentModel.{nameof(System.ComponentModel.TypeConverter)}";
    public const string StringConverter = $"global::{nameof(System)}.ComponentModel.{nameof(System.ComponentModel.StringConverter)}";
    public const string TypeDescriptor = $"global::{nameof(System)}.ComponentModel.{nameof(System.ComponentModel.TypeDescriptor)}";
    
    public const string Regex = $"global::{nameof(System)}.{nameof(System.Text)}.{nameof(System.Text.RegularExpressions)}.{nameof(System.Text.RegularExpressions.Regex)}";
    public const string Match = $"global::{nameof(System)}.{nameof(System.Text)}.{nameof(System.Text.RegularExpressions)}.{nameof(System.Text.RegularExpressions.Match)}";
    
    public const string ArgumentException = $"global::{nameof(System)}.{nameof(System.ArgumentException)}";
}