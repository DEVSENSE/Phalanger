//© Đonny 2009. part of Phalanger project
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger {
    internal class FieldWrapper {
        private DebuggerContext context;
        public DebuggerContext Context { get { return context; } }
        private IDebugField field;
        public IDebugField Field { get { return field; } }
        public FieldWrapper(DebuggerContext context, IDebugField field) {
            this.field = field;
            this.context = context;
        }
        public FIELD_INFO GetInfo() {
            FIELD_INFO[] info = new FIELD_INFO[1];
            NativeMethods.ThrowOnFailure(
                Field.GetInfo((uint)enum_FIELD_INFO_FIELDS.FIF_ALL, info));
            return info[0];
        }
        public TYPE_INFO GetTypeInfo() {
            TYPE_INFO[] info = new TYPE_INFO[1];
            NativeMethods.ThrowOnFailure(
                Field.GetTypeInfo(info));
            return info[0];
        }
        public FieldWrapper GetFieldType() {
            IDebugField type;
            NativeMethods.ThrowOnFailure(
                Field.GetType(out type));
            return new FieldWrapper(Context, type);
        }
        public ObjectWrapper GetValue() {
            IDebugContainerField container;
            Field.GetContainer(out container);
            FieldWrapper containerwrapper = null;
            if(container!=null)
                containerwrapper = new FieldWrapper(Context,container);
            IDebugObject obj;
            NativeMethods.ThrowOnFailure(
                Context.Binder.Bind(containerwrapper.GetValue().Object,Field,out obj)); 
            return new ObjectWrapper(context,obj);
        }
        public PropertyWrapper GetProperty() {
            PropertyFactory factory = new PropertyFactory(Context);
            PropertyBase pprop;
            NativeMethods.ThrowOnFailure(
                factory.CreateProperty(new PropertyContext(context), Field, out pprop));
            return new PropertyWrapper(context, pprop,null);//TODO: Better object than null 
        }

        /// <summary>If field being wrapped if <see cref="IDebugMethodField"/> returns its locals and arguments</summary>
        /// <returns>Locals and arguments of metthod being wrapped. If field being wrapped is not <see cref="IDebugMethodField"/> returns null</returns>
        public FieldWrapper[] GetLocals(){
            if(this.Field is IDebugMethodField) {
                IDebugMethodField Field = (IDebugMethodField) this.Field;
                IEnumDebugFields ppLocals;
                NativeMethods.ThrowOnFailure(
                    Field.EnumAllLocals(context.Address,out ppLocals));
                uint count;
                NativeMethods.ThrowOnFailure(
                    ppLocals.GetCount(out count));
                if(count == 0) return new FieldWrapper[0];
                IDebugField[] fields = new IDebugField[count];
                NativeMethods.ThrowOnFailure(
                    ppLocals.Next(count,fields,ref count));
                if(count == 0) return new FieldWrapper[0];
                FieldWrapper[] ret = new FieldWrapper[count];
                for(int i = 0; i < count; i++)
                    ret[i] = new FieldWrapper(Context, fields[i]);
                return ret;
            }else return null;
        }
        public FieldWrapper GetLocal(string name) {
            foreach(FieldWrapper f in GetLocals())
                if(f.Name == name) return f;
            return null;
        }

        public string Name { get { return GetInfo().bstrName; } }
        public string FullName { get { return GetInfo().bstrFullName; } }
        public string TypeName { get { return GetInfo().bstrType; } }
        public override string ToString() {
            return FullName;
        }

        //public static implicit operator IDebugField(FieldWrapper a) {
        //    return a.Field;
        //}
        public static implicit operator EEDebugField(FieldWrapper a) {
            return new EEDebugField(a.Field);
        }
    }
}
