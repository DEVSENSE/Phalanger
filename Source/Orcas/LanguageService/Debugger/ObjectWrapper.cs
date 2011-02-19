//© Đonny 2009. part of Phalanger project
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace PHP.VisualStudio.PhalangerLanguageService.Debugger {
    /// <summary>Wraps <see cref="IDebugObject"/> to provide better access</summary>
    internal class ObjectWrapper :IDisposable  {
        /// <summary>Contains value of the <see cref="Object"/> property</summary>
        private IDebugObject @object;
        /// <summary>Object beiong wrapped</summary>
        public IDebugObject Object { get { return @object; } }
        /// <summary>Contains value of the <see cref="Context"/> property</summary>
        private DebuggerContext context;
        /// <summary>Debugger context of object</summary>
        public DebuggerContext Context { get { return context; } }
        /// <summary>Creates new instance of the <see cref="PropertyWrapper"/> class</summary>
        /// <param name="context">debugger context</param>
        /// <param name="object">Object to wrap</param>
        public ObjectWrapper(DebuggerContext context, IDebugObject @object) {
            this.@object = @object;
            this.context = context;
        }
        /// <summary>Gets property for this object</summary>
        public PropertyWrapper GetProperty() {
            EEDebugObject eeobj = new EEDebugObject(Object);
            var ot = GetObjectType();
            return new PropertyWrapper(context,
                new ObjectProperty(new PropertyContext(context), ot, eeobj, ot), Object);
        }
        /// <summary>Gets tyxe of object</summary>
        public EEDebugField GetObjectType() {
            EEDebugField typefield;
            EEDebugObject eeobj = new EEDebugObject(Object);
            NativeMethods.ThrowOnFailure(
                eeobj.GetType(context, out typefield));
            return typefield;
        }
        /// <summary>Gets strign represnetation</summary>
        /// <returns>"<see cref="GetProperty">GetProperty</see>.<see cref="PropertyWrapper.FullName">FullName</see> (<see cref="GetProperty">GetProperty</see>.<see cref="PropertyWrapper.ValueString">ValueString</see>)"</returns>
        public override string ToString() {
            PropertyWrapper prp = GetProperty();
            return string.Format("{0} ({1})", prp.FullName, prp.ValueString);
        }
        /// <summary>Gets managed object for this object</summary>
        public ManagedObjectWrapper GetManagedObject() {
            IDebugManagedObject imdo;
            NativeMethods.ThrowOnFailure(
                Object.GetManagedDebugObject(out imdo));
            return new ManagedObjectWrapper(context, imdo);
        }
        //public static implicit operator IDebugObject(ObjectWrapper a) {
        //    return a.Object;
        //}
        public static implicit operator EEDebugObject(ObjectWrapper a) {
            return new EEDebugObject(a.Object);
        }

        #region IDisposable Members

        public void Dispose() {
            context = null;
            @object = null;
        }

        #endregion
    }
    /// <summary>Wraps <see cref="IDebugManagedObject"/> to provide better access</summary>
    internal class ManagedObjectWrapper {
        /// <summary>Contains value of the <see cref="Context"/> property</summary>
        private DebuggerContext context;
        /// <summary>Debugger context of object</summary>
        public DebuggerContext Context { get { return context; } }
        /// <summary>Contains value of the <see cref="Object"/> property</summary>
        private IDebugManagedObject @object;
        /// <summary>Managed object beibng wrapped</summary>
        public IDebugManagedObject Object { get { return @object; } }
        /// <summary>CTor creates new instance of the <see cref="ManagedObjectWrapper"/>  class</summary>
        /// <param name="context">Debugger context</param>
        /// <param name="object">Managed object to wrap</param>
        public ManagedObjectWrapper(DebuggerContext context, IDebugManagedObject @object) {
            this.@object = @object;
            this.context = context;
        }
        /// <summary>Gets obiect value as instance of managed class</summary>
        public object GetValue() {
            object value;
            NativeMethods.ThrowOnFailure(
                this.Object.GetManagedObject(out value));
            return value;
        }
        /// <summary>String representation</summary>
        /// <returns>"<see cref="GetValue">GetValue</see>.<see cref="Object.GetType">GetType</see>.<see cref="Type.FullName">FullName</see> (<see cref="GetValue">GetValue</see>.<see cref="Object.ToString">ToString</see>)"</returns>
        public override string ToString() {
            object value = GetValue();
            return string.Format("{0} ({1})", value.GetType().FullName, value.ToString());
        }
        //public static implicit operator IDebugManagedObject(ManagedObjectWrapper a) {
        //    return a.Object;
        //}
    }
}
