//© Đonny 2009. part of Phalanger project
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger {
    internal class PropertyWrapper   {
        private IDebugProperty2 property;
        public IDebugProperty2 Property { get { return property; } }
        private DebuggerContext context;
        public DebuggerContext Context { get { return context; } }
        private IDebugObject owner;
        public IDebugObject Owner { get { return owner; } }
        /// <summary>Creates new instance of the <see cref="PropertyWrapper"/> class</summary>
        /// <param name="context">debugger context</param>
        /// <param name="property">Property to wrap</param>
        /// <param name="owner">Property owner (can be null)</param>
        public PropertyWrapper(DebuggerContext context, IDebugProperty2 property, IDebugObject owner) {
            this.property = property;
            this.context = context;
        }
        public PropertyWrapper(DebuggerContext context, IDebugObject @object,IDebugField baseField) :
            this(context,new ObjectProperty(
                new PropertyContext(context),
                new EEDebugField(baseField),
                new EEDebugObject(@object),
                new EEDebugField(new FieldWrapper(context,baseField).GetFieldType().Field)),
                @object){}
        public DEBUG_PROPERTY_INFO[] GetChildren (){
            IEnumDebugPropertyInfo2 ppEnum;            
            NativeMethods.ThrowOnFailure(
                Property.EnumChildren(
                    (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL,
                    0,ref FilterGuids.AllLocals, 0, null, 0,out ppEnum));
            uint count;
            NativeMethods.ThrowOnFailure(
                ppEnum.GetCount(out count));
            if(count == 0) return new DEBUG_PROPERTY_INFO[0];
            DEBUG_PROPERTY_INFO[] info = new DEBUG_PROPERTY_INFO[count];
            NativeMethods.ThrowOnFailure(
                ppEnum.Next(count, info, out count));
            if(count == 0) return new DEBUG_PROPERTY_INFO[0];
            if(count == info.Length) return info;
            DEBUG_PROPERTY_INFO[] ret = new DEBUG_PROPERTY_INFO[count];
            Array.ConstrainedCopy(info, 0, ret, 0, ret.Length);
            return ret;
        }
        public DEBUG_PROPERTY_INFO? GetChild(string name){
            foreach (var Child in GetChildren())
                if(Child.bstrName==name) return Child;
            return null;
        }
        public DEBUG_PROPERTY_INFO GetPropertyInfo() {
            DEBUG_PROPERTY_INFO[] info = new DEBUG_PROPERTY_INFO[1];
            NativeMethods.ThrowOnFailure(
                Property.GetPropertyInfo(
                    (uint)enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ALL,
                    0,0,null,0,info));
            return info[0];
        }
        public string Name {
            get { return GetPropertyInfo().bstrName; }
        }
        public string FullName {
            get { return GetPropertyInfo().bstrFullName; }
        }
        public string TypeName {
            get { return GetPropertyInfo().bstrType; }
        }
        public string ValueString {
            get { return GetPropertyInfo().bstrValue; }
        }
        public override string ToString() {
            return string.Format("{0} ({1})", FullName, ValueString);
        }

        //public static implicit operator IDebugProperty(PropertyWrapper a) {
        //    return a.Property;
        //}
    }
}
