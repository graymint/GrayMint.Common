﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GrayMint.Common.AspNetCore {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class GrayMintResource {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal GrayMintResource() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("GrayMint.Common.AspNetCore.GrayMintResource", typeof(GrayMintResource).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /*Perform a &apos;USE &lt;database name&gt;&apos; to select the database in which to run the script.*/  
        ///-- Declare variables  
        ///SET NOCOUNT ON;  
        ///DECLARE @tablename VARCHAR(255);  
        ///DECLARE @execstr   VARCHAR(400);  
        ///DECLARE @objectid  INT;  
        ///DECLARE @indexid   INT;  
        ///DECLARE @frag      DECIMAL;  
        ///DECLARE @maxfrag   DECIMAL;  
        ///  
        ///-- Decide on the maximum fragmentation to allow for.  
        ///SELECT @maxfrag = 30.0;  
        ///  
        ///-- Declare a cursor.  
        ///DECLARE tables CURSOR FOR  
        ///   SELECT TABLE_SCHEMA + &apos;.&apos; + TABLE_NAME  
        ///   [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string SqlMaintenance {
            get {
                return ResourceManager.GetString("SqlMaintenance", resourceCulture);
            }
        }
    }
}
