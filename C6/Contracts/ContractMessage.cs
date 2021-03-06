﻿// This file is part of the C6 Generic Collection Library for C# and CLI
// See https://github.com/C6/C6/blob/master/LICENSE.md for licensing details.

namespace C6.Contracts
{
    public static class ContractMessage
    {
        // Collection state
        public static string CollectionMustBeNonReadOnly => "Collection must be non-read-only";
        public static string CollectionMustBeNonFixedSize => "Collection must be non-fixed-sized";
        public static string CollectionMustBeNonEmpty => "Collection must be non-empty";
        public static string CollectionMustAllowDuplicates => "Duplicates are not allowed";
        public static string CollectionMustContainItem => "Collection must contain item";
        public static string CollectionMustContainArgument => "Collection must contain argument";

        // Events
        public static string EventMustBeListenable => "Event must be listenable";
        public static string EventMustBeActive => "Event must be active";
        public static string EventArgumentMustMatchEventType => "Event arguments must match event type";

        // Parameter validation
        public static string ArgumentMustBeNonNull => "Argument must be non-null";
        public static string ItemMustBeNonNull => "Item must be non-null if collection disallows null values";
        public static string ItemsMustBeNonNull => "All items must be non-null if collection disallows null values";
        public static string ArgumentMustBeNonNegative => "Argument must be non-negative";
        public static string ArgumentMustBePositive => "Argument must be positive";
        public static string ArgumentMustBeWithinBounds => "Argument must be within bounds";

        public static string StringMustBeNonEmpty => "String must be non-empty";

        public static string EnumMustBeDefined => "Enum value must be defined";

        public static string AllowsNullMustBeFalseForValueTypes => "Value types cannot be null";
        public static string TypeMustBeStruct => "Generic type must be a structure.";

        // View
        public static string MustBeValid => "The list or view must be valid";
        public static string NotAView => "Not a view";
        public static string UnderlyingListMustBeTheSame = "The underlying list must be the same";        
    }
}