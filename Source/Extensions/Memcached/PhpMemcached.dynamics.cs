using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ComponentModel;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.Memcached
{
    [Serializable()]
    public partial class Memcached : PhpObject
    {
        #region ctors

        /// <summary></summary>
        protected Memcached(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        /// <summary></summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Memcached(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {

        }
        /// <summary></summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Memcached(ScriptContext context, DTypeDesc caller)
            : this(context, true)
        {
            this.InvokeConstructor(context, caller);
        }

        #endregion

        #region member methods

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object __construct(ScriptContext __context, [Optional]object persistent_id)
        {
            string tmp1 = string.Empty;
            if (persistent_id != Arg.Default)
            {
                if ((tmp1 = PhpVariable.AsString(persistent_id)) == null)
                {
                    PhpException.InvalidImplicitCast(persistent_id, "string", "__construct");
                    return null;
                }
            }

            __construct(tmp1);
            return null;
        }
        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            stack.CalleeName = "__construct";

            object arg1 = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((Memcached)instance).__construct(stack.Context, arg1);
        }
        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object add(ScriptContext __context, object key, object value, [Optional]object expiration)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "add");
                return null;
            }

            // 2
            object arg2 = value;

            // 3
            int arg3 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg3 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "add");
                    return null;
                }
            }

            // call
            return add(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object add(object instance, PhpStack stack)
        {
            stack.CalleeName = "add";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((Memcached)instance).add(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object addByKey(ScriptContext __context, object server_key, object key, object value, [Optional]object expiration)
        {
            // 1
            string arg1 = PhpVariable.AsString(server_key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "addByKey");
                return null;
            }

            // 2
            string arg2 = PhpVariable.AsString(key);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "addByKey");
                return null;
            }

            // 3
            object arg3 = value;

            // 4
            int arg4 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg4 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "addByKey");
                    return null;
                }
            }

            // call
            return addByKey(arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object addByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "addByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            object arg4 = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((Memcached)instance).addByKey(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object addServer(ScriptContext __context, object host, object port, [Optional]object weight)
        {
            // 1
            string arg1 = PhpVariable.AsString(host);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(host, "string", "addServer");
                return null;
            }

            // 2
            int arg2;
            if (port is int)
                arg2 = (int)port;
            else
            {
                PhpException.InvalidImplicitCast(port, "string", "addServer");
                return null;
            }

            // 3
            int arg3 = 0;
            if (weight != Arg.Default)
            {
                if (weight is int)
                    arg3 = (int)weight;
                else
                {
                    PhpException.InvalidImplicitCast(weight, "int", "addServer");
                    return null;
                }
            }

            // call
            return addServer(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object addServer(object instance, PhpStack stack)
        {
            stack.CalleeName = "addServer";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((Memcached)instance).addServer(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object addServers(ScriptContext __context, object servers)
        {
            // 1
            PhpArray arg1 = servers as PhpArray;

            // call
            return addServers(arg1);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object addServers(object instance, PhpStack stack)
        {
            stack.CalleeName = "addServers";

            object arg1 = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((Memcached)instance).addServers(stack.Context, arg1);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object append(ScriptContext __context, object key, object value)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "append");
                return null;
            }

            // 2
            string arg2 = PhpVariable.AsString(value);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(value, "string", "append");
                return null;
            }

            // call
            return append(arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object append(object instance, PhpStack stack)
        {
            stack.CalleeName = "append";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((Memcached)instance).append(stack.Context, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object appendByKey(ScriptContext __context, object server_key, object key, object value)
        {
            // 1
            string arg1 = PhpVariable.AsString(server_key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "appendByKey");
                return null;
            }

            // 2
            string arg2 = PhpVariable.AsString(key);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "appendByKey");
                return null;
            }

            // 3
            string arg3 = PhpVariable.AsString(value);
            if (arg3 == null)
            {
                PhpException.InvalidImplicitCast(value, "string", "appendByKey");
                return null;
            }

            // call
            return appendByKey(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object appendByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "appendByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            stack.RemoveFrame();
            return ((Memcached)instance).appendByKey(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object cas(ScriptContext __context, object cas_token, object key, object value, [Optional]object expiration)
        {
            // 1
            if (!(cas_token is float))
            {
                PhpException.InvalidImplicitCast(cas_token, "float", "cas");
                return null;
            }
            float arg1 = (float)cas_token;

            // 2
            string arg2 = PhpVariable.AsString(key);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "cas");
                return null;
            }

            // 3
            object arg3 = value;

            // 4
            int arg4 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg4 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "cas");
                    return null;
                }
            }

            // call
            return cas(arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object cas(object instance, PhpStack stack)
        {
            stack.CalleeName = "cas";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            object arg4 = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((Memcached)instance).cas(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object casByKey(ScriptContext __context, object cas_token, object server_key, object key, object value, [Optional]object expiration)
        {
            // 1
            if (!(cas_token is float))
            {
                PhpException.InvalidImplicitCast(cas_token, "float", "cas");
                return null;
            }
            float arg1 = (float)cas_token;

            // 1,5
            string arg1a = PhpVariable.AsString(server_key);
            if (arg1a == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "casByKey");
                return null;
            }

            // 2
            string arg2 = PhpVariable.AsString(key);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "casByKey");
                return null;
            }

            // 3
            object arg3 = value;

            // 4
            int arg4 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg4 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "casByKey");
                    return null;
                }
            }

            // call
            return casByKey(arg1, arg1a, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object casByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "cas";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            object arg4 = stack.PeekValue(4);
            object arg5 = stack.PeekValueOptional(5);
            stack.RemoveFrame();
            return ((Memcached)instance).casByKey(stack.Context, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object decrement(ScriptContext __context, object key, [Optional]object offset)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "decrement");
                return null;
            }

            // 2
            int arg2 = 1;
            if (offset != Arg.Default)
            {
                if (offset is int)
                    arg2 = (int)offset;
                else
                {
                    PhpException.InvalidImplicitCast(offset, "int", "decrement");
                    return null;
                }
            }

            // call
            int x = decrement(arg1, arg2);
            return (x != -1) ? x : (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object decrement(object instance, PhpStack stack)
        {
            stack.CalleeName = "decrement";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((Memcached)instance).decrement(stack.Context, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object delete(ScriptContext __context, object key, [Optional]object time)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "delete");
                return null;
            }

            // 2
            int arg2 = 0;
            if (time != Arg.Default)
            {
                if (time is int)
                    arg2 = (int)time;
                else
                {
                    PhpException.InvalidImplicitCast(time, "int", "delete");
                    return null;
                }
            }

            // call
            return delete(arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object delete(object instance, PhpStack stack)
        {
            stack.CalleeName = "delete";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((Memcached)instance).delete(stack.Context, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object deleteByKey(ScriptContext __context, object server_key, object key, [Optional]object time)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "deleteByKey");
                return null;
            }

            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "deleteByKey");
                return null;
            }

            // 2
            int arg2 = 0;
            if (time != Arg.Default)
            {
                if (time is int)
                    arg2 = (int)time;
                else
                {
                    PhpException.InvalidImplicitCast(time, "int", "deleteByKey");
                    return null;
                }
            }

            // call
            return deleteByKey(arg0, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object deleteByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "deleteByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((Memcached)instance).deleteByKey(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object fetch(ScriptContext __context)
        {
            // call
            return fetch() ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object fetch(object instance, PhpStack stack)
        {
            stack.CalleeName = "fetch";

            stack.RemoveFrame();
            return ((Memcached)instance).fetch(stack.Context);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object fetchAll(ScriptContext __context)
        {
            // call
            return fetchAll() ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object fetchAll(object instance, PhpStack stack)
        {
            stack.CalleeName = "fetchAll";

            stack.RemoveFrame();
            return ((Memcached)instance).fetchAll(stack.Context);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object flush(ScriptContext __context, [Optional]object delay)
        {
            int arg1 = 0;
            if (delay != Arg.Default)
            {
                if (delay is int)
                    arg1 = (int)delay;
                else
                {
                    PhpException.InvalidImplicitCast(delay, "int", "flush");
                    return null;
                }
            }

            // call
            return flush();
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object flush(object instance, PhpStack stack)
        {
            stack.CalleeName = "flush";

            object arg1 = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((Memcached)instance).flush(stack.Context, arg1);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object get(ScriptContext __context, object key, [Optional]object cache_cb, [Optional]object cas_token)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "get");
                return null;
            }

            // 2
            PhpCallback arg2 = cache_cb as PhpCallback;

            // 3
            PhpReference arg3 = cas_token as PhpReference;

            // call
            return get(arg1, arg2, arg3) ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object get(object instance, PhpStack stack)
        {
            stack.CalleeName = "get";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            object arg3 = stack.PeekReferenceOptional(3);

            stack.RemoveFrame();
            return ((Memcached)instance).get(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getByKey(ScriptContext __context, object server_key, object key, [Optional]object cache_cb, [Optional]object cas_token)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "getByKey");
                return null;
            }

            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "getByKey");
                return null;
            }

            // 2
            PhpCallback arg2 = cache_cb as PhpCallback;

            // 3
            PhpReference arg3 = cas_token as PhpReference;

            // call
            return getByKey(arg0, arg1, arg2, arg3) ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "getByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            object arg4 = stack.PeekReferenceOptional(4);

            stack.RemoveFrame();
            return ((Memcached)instance).getByKey(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getDelayed(ScriptContext __context, object keys, [Optional]object with_cas, [Optional]object value_cb)
        {
            // 1
            PhpArray arg1 = keys as PhpArray;

            // 2
            bool arg2 = false;
            if (with_cas != Arg.Default)
            {
                if (with_cas is bool)
                    arg2 = (bool)with_cas;
                else
                {
                    PhpException.InvalidImplicitCast(with_cas, "bool", "getDelayed");
                    return null;
                }
            }

            // 3
            PhpCallback arg3 = value_cb as PhpCallback;

            // call
            return getDelayed(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getDelayed(object instance, PhpStack stack)
        {
            stack.CalleeName = "getDelayed";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            object arg3 = stack.PeekValueOptional(3);

            stack.RemoveFrame();
            return ((Memcached)instance).getDelayed(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getDelayedByKey(ScriptContext __context, object server_key, object keys, [Optional]object with_cas, [Optional]object value_cb)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "getByKey");
                return null;
            }

            // 1
            PhpArray arg1 = keys as PhpArray;

            // 2
            bool arg2 = false;
            if (with_cas != Arg.Default)
            {
                if (with_cas is bool)
                    arg2 = (bool)with_cas;
                else
                {
                    PhpException.InvalidImplicitCast(with_cas, "bool", "getDelayedByKey");
                    return null;
                }
            }

            // 3
            PhpCallback arg3 = value_cb as PhpCallback;

            // call
            return getDelayed(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getDelayedByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "getDelayedByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            object arg4 = stack.PeekValueOptional(4);

            stack.RemoveFrame();
            return ((Memcached)instance).getDelayedByKey(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getMulti(ScriptContext __context, object keys, [Optional]object cas_tokens, [Optional]object flags)
        {
            // 1
            PhpArray arg1 = keys as PhpArray;

            // 2
            PhpReference arg2 = cas_tokens as PhpReference;

            // 3
            int arg3 = 0;
            if (flags != Arg.Default)
            {
                if (flags is int)
                    arg3 = (int)flags;
                else
                {
                    PhpException.InvalidImplicitCast(flags, "int", "getMulti");
                    return null;
                }
            }

            // call
            return getMulti(arg1, arg2, arg3) ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getMulti(object instance, PhpStack stack)
        {
            stack.CalleeName = "getMulti";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekReferenceOptional(2);
            object arg3 = stack.PeekValueOptional(3);

            stack.RemoveFrame();
            return ((Memcached)instance).getMulti(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getMultiByKey(ScriptContext __context, object server_key, object keys, [Optional]object cas_tokens, [Optional]object flags)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "getByKey");
                return null;
            }

            // 1
            PhpArray arg1 = keys as PhpArray;

            // 2
            PhpReference arg2 = cas_tokens as PhpReference;

            // 3
            int arg3 = 0;
            if (flags != Arg.Default)
            {
                if (flags is int)
                    arg3 = (int)flags;
                else
                {
                    PhpException.InvalidImplicitCast(flags, "int", "getMulti");
                    return null;
                }
            }

            // call
            return getMultiByKey(arg0, arg1, arg2, arg3) ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getMultiByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "getMultiByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekReferenceOptional(3);
            object arg4 = stack.PeekValueOptional(4);

            stack.RemoveFrame();
            return ((Memcached)instance).getMultiByKey(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getOption(ScriptContext __context, object option)
        {
            // 1
            int arg1;
            if (option is int)
                arg1 = (int)option;
            else
            {
                PhpException.InvalidImplicitCast(option, "int", "getOption");
                return null;
            }

            // call
            return getOption(arg1) ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getOption(object instance, PhpStack stack)
        {
            stack.CalleeName = "getOption";

            object arg1 = stack.PeekValue(1);

            stack.RemoveFrame();
            return ((Memcached)instance).getOption(stack.Context, arg1);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getResultCode(ScriptContext __context)
        {
            // call
            return getResultCode();
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getResultCode(object instance, PhpStack stack)
        {
            stack.CalleeName = "getResultCode";

            stack.RemoveFrame();
            return ((Memcached)instance).getResultCode(stack.Context);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getResultMessage(ScriptContext __context)
        {
            // call
            return getResultMessage();
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getResultMessage(object instance, PhpStack stack)
        {
            stack.CalleeName = "getResultMessage";

            stack.RemoveFrame();
            return ((Memcached)instance).getResultMessage(stack.Context);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getServerByKey(ScriptContext __context, object server_key)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "getByKey");
                return null;
            }

            // call
            return getServerByKey(arg0) ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getServerByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "getServerByKey";

            object arg1 = stack.PeekValue(1);

            stack.RemoveFrame();
            return ((Memcached)instance).getServerByKey(stack.Context, arg1);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getServerList(ScriptContext __context)
        {
            // call
            return getServerList() ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getServerList(object instance, PhpStack stack)
        {
            stack.CalleeName = "getServerList";

            stack.RemoveFrame();
            return ((Memcached)instance).getServerList(stack.Context);
        }
        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getStats(ScriptContext __context)
        {
            // call
            return getStats() ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStats(object instance, PhpStack stack)
        {
            stack.CalleeName = "getStats";

            stack.RemoveFrame();
            return ((Memcached)instance).getStats(stack.Context);
        }


        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object getVersion(ScriptContext __context)
        {
            // call
            return getVersion() ?? (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getVersion(object instance, PhpStack stack)
        {
            stack.CalleeName = "getVersion";

            stack.RemoveFrame();
            return ((Memcached)instance).getVersion(stack.Context);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object increment(ScriptContext __context, object key, [Optional]object offset)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "increment");
                return null;
            }

            // 2
            int arg2 = 1;
            if (offset != Arg.Default)
            {
                if (offset is int)
                    arg2 = (int)offset;
                else
                {
                    PhpException.InvalidImplicitCast(offset, "int", "increment");
                    return null;
                }
            }

            // call
            int x = increment(arg1, arg2);
            return (x != -1) ? x : (object)false;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object increment(object instance, PhpStack stack)
        {
            stack.CalleeName = "increment";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((Memcached)instance).increment(stack.Context, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object prepend(ScriptContext __context, object key, object value)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "prepend");
                return null;
            }

            // 2
            string arg2 = PhpVariable.AsString(value);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(value, "string", "prepend");
                return null;
            }

            // call
            return prepend(arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object prepend(object instance, PhpStack stack)
        {
            stack.CalleeName = "prepend";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((Memcached)instance).prepend(stack.Context, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object prependByKey(ScriptContext __context, object server_key, object key, object value)
        {
            // 1
            string arg1 = PhpVariable.AsString(server_key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "prependByKey");
                return null;
            }

            // 2
            string arg2 = PhpVariable.AsString(key);
            if (arg2 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "prependByKey");
                return null;
            }

            // 3
            string arg3 = PhpVariable.AsString(value);
            if (arg3 == null)
            {
                PhpException.InvalidImplicitCast(value, "string", "prependByKey");
                return null;
            }

            // call
            return prependByKey(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object prependByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "prependByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            stack.RemoveFrame();
            return ((Memcached)instance).prependByKey(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object replace(ScriptContext __context, object key, object value, [Optional]object expiration)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "replace");
                return null;
            }

            // 2
            object arg2 = value;

            // 3
            int arg3 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg3 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "replace");
                    return null;
                }
            }

            // call
            return replace(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object replace(object instance, PhpStack stack)
        {
            stack.CalleeName = "replace";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((Memcached)instance).replace(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object replaceByKey(ScriptContext __context, object server_key, object key, object value, [Optional]object expiration)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "replaceByKey");
                return null;
            }

            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "replaceByKey");
                return null;
            }

            // 2
            object arg2 = value;

            // 3
            int arg3 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg3 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "replaceByKey");
                    return null;
                }
            }

            // call
            return replaceByKey(arg0, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object replaceByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "replaceByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            object arg4 = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((Memcached)instance).replaceByKey(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object set(ScriptContext __context, object key, object value, [Optional]object expiration)
        {
            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "set");
                return null;
            }

            // 2
            object arg2 = value;

            // 3
            int arg3 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg3 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "set");
                    return null;
                }
            }

            // call
            return set(arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object set(object instance, PhpStack stack)
        {
            stack.CalleeName = "set";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((Memcached)instance).set(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object setByKey(ScriptContext __context, object server_key, object key, object value, [Optional]object expiration)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "setByKey");
                return null;
            }

            // 1
            string arg1 = PhpVariable.AsString(key);
            if (arg1 == null)
            {
                PhpException.InvalidImplicitCast(key, "string", "setByKey");
                return null;
            }

            // 2
            object arg2 = value;

            // 3
            int arg3 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg3 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "setByKey");
                    return null;
                }
            }

            // call
            return setByKey(arg0, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "setByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValue(3);
            object arg4 = stack.PeekValueOptional(4);
            stack.RemoveFrame();
            return ((Memcached)instance).setByKey(stack.Context, arg1, arg2, arg3, arg4);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object setMulti(ScriptContext __context, object items, [Optional]object expiration)
        {
            // 1
            PhpArray arg1 = items as PhpArray;

            // 2
            int arg2 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg2 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "setMulti");
                    return null;
                }
            }

            // call
            return setMulti(arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setMulti(object instance, PhpStack stack)
        {
            stack.CalleeName = "setMulti";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((Memcached)instance).setMulti(stack.Context, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object setMultiByKey(ScriptContext __context, object server_key, object items, [Optional]object expiration)
        {
            // 0
            string arg0 = PhpVariable.AsString(server_key);
            if (arg0 == null)
            {
                PhpException.InvalidImplicitCast(server_key, "string", "setMultiByKey");
                return null;
            }

            // 1
            PhpArray arg1 = items as PhpArray;

            // 2
            int arg2 = 0;
            if (expiration != Arg.Default)
            {
                if (expiration is int)
                    arg2 = (int)expiration;
                else
                {
                    PhpException.InvalidImplicitCast(expiration, "int", "setMultiByKey");
                    return null;
                }
            }

            // call
            return setMultiByKey(arg0, arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setMultiByKey(object instance, PhpStack stack)
        {
            stack.CalleeName = "setMultiByKey";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            object arg3 = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((Memcached)instance).setMultiByKey(stack.Context, arg1, arg2, arg3);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object setOption(ScriptContext __context, object option, object value)
        {
            // 1
            int arg1;
            if (option is int)
                arg1 = (int)option;
            else
            {
                PhpException.InvalidImplicitCast(option, "int", "setOption");
                return null;
            }

            // 2
            object arg2 = value;

            // call
            return setOption(arg1, arg2);
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setOption(object instance, PhpStack stack)
        {
            stack.CalleeName = "setOption";

            object arg1 = stack.PeekValue(1);
            object arg2 = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((Memcached)instance).setOption(stack.Context, arg1, arg2);
        }

        #endregion

        #region PHP class constants

        /// <summary>
        /// Enables or disables payload compression. When enabled, item values longer than a certain threshold (currently 100 bytes) will be compressed during storage and decompressed during retrieval transparently.
        /// Type: boolean, default: TRUE.
        /// </summary>
        public static readonly object OPT_COMPRESSION = (int)OptionsConstants.Compression;

        /// <summary>
        /// Specifies the serializer to use for serializing non-scalar values. The valid serializers are Memcached::SERIALIZER_PHP or Memcached::SERIALIZER_IGBINARY. The latter is supported only when memcached is configured with --enable-memcached-igbinary option and the igbinary extension is loaded.
        /// Type: integer, default: Memcached::SERIALIZER_PHP.
        /// </summary>
        public static readonly object OPT_SERIALIZER = (int)OptionsConstants.Serializer;

        /// <summary>
        /// his can be used to create a "domain" for your item keys. The value specified here will be prefixed to each of the keys. It cannot be longer than 128 characters and will reduce the maximum available key size. The prefix is applied only to the item keys, not to the server keys.
        /// Type: string, default: "".
        /// </summary>
        public static readonly object OPT_PREFIX_KEY = (int)OptionsConstants.PrefixKey;

        /// <summary>
        /// pecifies the hashing algorithm used for the item keys. The valid values are supplied via Memcached::HASH_* constants. Each hash algorithm has its advantages and its disadvantages. Go with the default if you don't know or don't care.
        /// Type: integer, default: Memcached::HASH_DEFAULT
        /// </summary>
        public static readonly object OPT_HASH = (int)OptionsConstants.Hash;

        /// <summary>
        /// Specifies the method of distributing item keys to the servers. Currently supported methods are modulo and consistent hashing. Consistent hashing delivers better distribution and allows servers to be added to the cluster with minimal cache losses.
        /// Type: integer, default: Memcached::DISTRIBUTION_MODULA.
        /// </summary>
        public static readonly object OPT_DISTRIBUTION = (int)OptionsConstants.Distribution;

        /// <summary>
        /// Enables or disables compatibility with libketama-like behavior. When enabled, the item key hashing algorithm is set to MD5 and distribution is set to be weighted consistent hashing distribution. This is useful because other libketama-based clients (Python, Ruby, etc.) with the same server configuration will be able to access the keys transparently.
        /// Note:
        /// It is highly recommended to enable this option if you want to use consistent hashing, and it may be enabled by default in future releases.
        /// Type: boolean, default: FALSE.
        /// </summary>
        public static readonly object OPT_LIBKETAMA_COMPATIBLE = (int)OptionsConstants.LibketamaCompatible;

        /// <summary>
        /// Enables or disables buffered I/O. Enabling buffered I/O causes storage commands to "buffer" instead of being sent. Any action that retrieves data causes this buffer to be sent to the remote connection. Quitting the connection or closing down the connection will also cause the buffered data to be pushed to the remote connection.
        /// Type: boolean, default: FALSE.
        /// </summary>
        public static readonly object OPT_BUFFER_WRITES = (int)OptionsConstants.BufferWrites;

        /// <summary>
        /// Enable the use of the binary protocol. Please note that you cannot toggle this option on an open connection.
        /// Type: boolean, default: FALSE.
        /// </summary>
        public static readonly object OPT_BINARY_PROTOCOL = (int)OptionsConstants.BinaryProtocol;

        /// <summary>
        /// Enables or disables asynchronous I/O. This is the fastest transport available for storage functions.
        /// Type: boolean, default: FALSE.
        /// </summary>
        public static readonly object OPT_NO_BLOCK = (int)OptionsConstants.NoBlock;

        /// <summary>
        /// Enables or disables the no-delay feature for connecting sockets (may be faster in some environments).
        /// Type: boolean, default: FALSE.
        /// </summary>
        public static readonly object OPT_TCP_NODELAY = (int)OptionsConstants.TcpNoDelay;

        /// <summary>
        /// The maximum socket send buffer in bytes.
        /// Type: integer, default: varies by platform/kernel configuration.
        /// </summary>
        public static readonly object OPT_SOCKET_SEND_SIZE = (int)OptionsConstants.SocketSendSize;

        /// <summary>
        /// The maximum socket receive buffer in bytes.
        /// Type: integer, default: varies by platform/kernel configuration.
        /// </summary>
        public static readonly object OPT_SOCKET_RECV_SIZE = (int)OptionsConstants.SocketRecvSize;

        /// <summary>
        /// In non-blocking mode this set the value of the timeout during socket connection, in milliseconds.
        /// Type: integer, default: 1000.
        /// </summary>
        public static readonly object OPT_CONNECT_TIMEOUT = (int)OptionsConstants.ConnectTimeout;

        /// <summary>
        /// The amount of time, in seconds, to wait until retrying a failed connection attempt.
        /// Type: integer, default: 0.
        /// </summary>
        public static readonly object OPT_RETRY_TIMEOUT = (int)OptionsConstants.RetryTimeout;

        /// <summary>
        /// Socket sending timeout, in microseconds. In cases where you cannot use non-blocking I/O this will allow you to still have timeouts on the sending of data.
        /// Type: integer, default: 0.
        /// </summary>
        public static readonly object OPT_SEND_TIMEOUT = (int)OptionsConstants.SendTimeout;

        /// <summary>
        /// Socket reading timeout, in microseconds. In cases where you cannot use non-blocking I/O this will allow you to still have timeouts on the reading of data.
        /// Type: integer, default: 0.
        /// </summary>
        public static readonly object OPT_RECV_TIMEOUT = (int)OptionsConstants.RecvTimeout;

        /// <summary>
        /// Timeout for connection polling, in milliseconds.
        /// Type: integer, default: 1000.
        /// </summary>
        public static readonly object OPT_POLL_TIMEOUT = (int)OptionsConstants.PollTimeout;

        /// <summary>
        /// Enables or disables caching of DNS lookups.
        /// Type: boolean, default: FALSE.
        /// </summary>
        public static readonly object OPT_CACHE_LOOKUPS = (int)OptionsConstants.CacheLookups;

        /// <summary>
        /// Specifies the failure limit for server connection attempts. The server will be removed after this many continuous connection failures.
        /// Type: integer, default: 0.
        /// </summary>
        public static readonly object OPT_SERVER_FAILURE_LIMIT = (int)OptionsConstants.ServerFailureLimit;

        // HaveConstants

        /// <summary>
        /// Indicates whether igbinary serializer support is available.
        /// Type: boolean.
        /// </summary>
        public static readonly object HAVE_IGBINARY = false;

        /// <summary>
        /// Indicates whether JSON serializer support is available.
        /// Type: boolean.
        /// </summary>
        public static readonly object HAVE_JSON = true;

        // GetConstants

        /// <summary>
        /// A flag for Memcached::getMulti() and Memcached::getMultiByKey() to ensure that the keys are returned in the same order as they were requested in. Non-existing keys get a default value of NULL.
        /// </summary>
        public static readonly object GET_PRESERVE_ORDER = (int)GetConstants.PreserveOrder;

        // DistributionConstants

        /// <summary>
        /// Modulo-based key distribution algorithm.
        /// </summary>
        public static readonly object DISTRIBUTION_MODULA = (int)DistributionConstants.ModulA;

        /// <summary>
        /// Consistent hashing key distribution algorithm (based on libketama).
        /// </summary>
        public static readonly object DISTRIBUTION_CONSISTENT = (int)DistributionConstants.Consistent;

        // SerializerConstants

        /// <summary>
        /// The default PHP serializer.
        /// </summary>
        public static readonly object SERIALIZER_PHP = (int)SerializerConstants.Php;

        /// <summary>
        /// The » igbinary serializer. Instead of textual representation it stores PHP data structures in a compact binary form, resulting in space and time gains.
        /// </summary>
        public static readonly object SERIALIZER_IGBINARY = (int)SerializerConstants.IgBinary;

        /// <summary>
        /// The JSON serializer. 
        /// </summary>
        public static readonly object SERIALIZER_JSON = (int)SerializerConstants.JSON;

        // HashConstants

        /// <summary>
        /// The default (Jenkins one-at-a-time) item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_DEFAULT = (int)HashConstants.Default;
        
        /// <summary>
        /// MD5 item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_MD5 = (int)HashConstants.MD5;

        /// <summary>
        /// CRC item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_CRC = (int)HashConstants.CRC;

        /// <summary>
        /// FNV1_64 item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_FNV1_64 = (int)HashConstants.FNV1_64;

        /// <summary>
        /// FNV1_64A item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_FNV1A_64 = (int)HashConstants.FNV1A_32;

        /// <summary>
        /// FNV1_32 item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_FNV1_32 = (int)HashConstants.FNV1_32;

        /// <summary>
        /// FNV1_32A item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_FNV1A_32 = (int)HashConstants.FNV1A_32;

        /// <summary>
        /// Hsieh item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_HSIEH = (int)HashConstants.HSIEH;

        /// <summary>
        /// Murmur item key hashing algorithm.
        /// </summary>
        public static readonly object HASH_MURMUR = (int)HashConstants.MURMUR;

        // ResConstants

        /// <summary>
        /// The operation was successful.
        /// </summary>
        public static readonly object RES_SUCCESS = (int)ResConstants.Success;

        /// <summary>
        /// The operation failed in some fashion.
        /// </summary>
        public static readonly object RES_FAILURE = (int)ResConstants.Failure;

        /// <summary>
        /// DNS lookup failed.
        /// </summary>
        public static readonly object RES_HOST_LOOKUP_FAILURE = (int)ResConstants.HostLookupFailure;

        /// <summary>
        /// Failed to read network data.
        /// </summary>
        public static readonly object RES_UNKNOWN_READ_FAILURE = (int)ResConstants.UnknownReadFalure;

        /// <summary>
        /// Bad command in memcached protocol.
        /// </summary>
        public static readonly object RES_PROTOCOL_ERROR = (int)ResConstants.ProtocolError;

        /// <summary>
        /// Error on the client side.
        /// </summary>
        public static readonly object RES_CLIENT_ERROR = (int)ResConstants.ClientError;

        /// <summary>
        /// Error on the server side.
        /// </summary>
        public static readonly object RES_SERVER_ERROR = (int)ResConstants.ServerError;

        /// <summary>
        /// Failed to write network data.
        /// </summary>
        public static readonly object RES_WRITE_FAILURE = (int)ResConstants.WriteFailure;

        /// <summary>
        /// Failed to do compare-and-swap: item you are trying to store has been modified since you last fetched it.
        /// </summary>
        public static readonly object RES_DATA_EXISTS = (int)ResConstants.DataExists;

        /// <summary>
        /// Item was not stored: but not because of an error. This normally means that either the condition for an "add" or a "replace" command wasn't met, or that the item is in a delete queue.
        /// </summary>
        public static readonly object RES_NOTSTORED = (int)ResConstants.NotStored;

        /// <summary>
        /// Item with this key was not found (with "get" operation or "cas" operations).
        /// </summary>
        public static readonly object RES_NOTFOUND = (int)ResConstants.NotFound;

        /// <summary>
        /// Partial network data read error.
        /// </summary>
        public static readonly object RES_PARTIAL_READ = (int)ResConstants.PartialRead;

        /// <summary>
        /// Some errors occurred during multi-get.
        /// </summary>
        public static readonly object RES_SOME_ERRORS = (int)ResConstants.SomeErrors;

        /// <summary>
        /// Server list is empty.
        /// </summary>
        public static readonly object RES_NO_SERVERS = (int)ResConstants.NoServers;

        /// <summary>
        /// End of result set.
        /// </summary>
        public static readonly object RES_END = (int)ResConstants.End;

        /// <summary>
        /// System error.
        /// </summary>
        public static readonly object RES_ERRNO = (int)ResConstants.ErrNo;

        /// <summary>
        /// The operation was buffered.
        /// </summary>
        public static readonly object RES_BUFFERED = (int)ResConstants.Buffered;

        /// <summary>
        /// The operation timed out.
        /// </summary>
        public static readonly object RES_TIMEOUT = (int)ResConstants.Timeout;

        /// <summary>
        /// Bad key.
        /// </summary>
        public static readonly object RES_BAD_KEY_PROVIDED = (int)ResConstants.BadKeyProvided;

        /// <summary>
        /// Failed to create network socket.
        /// </summary>
        public static readonly object RES_CONNECTION_SOCKET_CREATE_FAILURE = (int)ResConstants.ConnectionSocketCreateFailure;

        /// <summary>
        /// Payload failure: could not compress/decompress or serialize/unserialize the value.
        /// </summary>
        public static readonly object RES_PAYLOAD_FAILURE = (int)ResConstants.PayloadFailure;

        #endregion

        #region __PopulateTypeDesc

        private static void __PopulateTypeDesc(PhpTypeDesc desc)
        {
            // methods
            desc.AddMethod("__construct", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.__construct));
            desc.AddMethod("add", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.add));
            desc.AddMethod("addByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.addByKey));
            desc.AddMethod("addServer", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.addServer));
            desc.AddMethod("addServers", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.addServers));
            desc.AddMethod("append", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.append));
            desc.AddMethod("appendByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.appendByKey));
            desc.AddMethod("cas", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.cas));
            desc.AddMethod("casByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.casByKey));
            desc.AddMethod("decrement", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.decrement));
            desc.AddMethod("delete", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.delete));
            desc.AddMethod("deleteByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.deleteByKey));
            desc.AddMethod("fetch", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.fetch));
            desc.AddMethod("fetchAll", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.fetchAll));
            desc.AddMethod("flush", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.flush));
            desc.AddMethod("get", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.get));
            desc.AddMethod("getByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getByKey));
            desc.AddMethod("getDelayed", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getDelayed));
            desc.AddMethod("getDelayedByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getDelayedByKey));
            desc.AddMethod("getMulti", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getMulti));
            desc.AddMethod("getMultiByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getMultiByKey));
            desc.AddMethod("getOption", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getOption));
            desc.AddMethod("getResultCode", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getResultCode));
            desc.AddMethod("getResultMessage", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getResultMessage));
            desc.AddMethod("getServerByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getServerByKey));
            desc.AddMethod("getServerList", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getServerList));
            desc.AddMethod("getStats", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getStats));
            desc.AddMethod("getVersion", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.getVersion));
            desc.AddMethod("increment", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.increment));
            desc.AddMethod("prepend", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.prepend));
            desc.AddMethod("prependByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.prependByKey));
            desc.AddMethod("replace", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.replace));
            desc.AddMethod("replaceByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.replaceByKey));
            desc.AddMethod("set", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.set));
            desc.AddMethod("setByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.setByKey));
            desc.AddMethod("setMulti", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.setMulti));
            desc.AddMethod("setMultiByKey", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.setMultiByKey));
            desc.AddMethod("setOption", PhpMemberAttributes.Public, new RoutineDelegate(Memcached.setOption));
            
            /*
            // OptionsConstants
            desc.AddConstant("OPT_COMPRESSION", OptionsConstants.Compression);
            desc.AddConstant("OPT_SERIALIZER", OptionsConstants.Serializer);
            desc.AddConstant("OPT_PREFIX_KEY", OptionsConstants.PrefixKey);
            desc.AddConstant("OPT_HASH", OptionsConstants.Hash);
            desc.AddConstant("OPT_DISTRIBUTION", OptionsConstants.Distribution);
            desc.AddConstant("OPT_LIBKETAMA_COMPATIBLE", OptionsConstants.LibketamaCompatible);
            desc.AddConstant("OPT_BUFFER_WRITES", OptionsConstants.BufferWrites);
            desc.AddConstant("OPT_BINARY_PROTOCOL", OptionsConstants.BinaryProtocol);
            desc.AddConstant("OPT_NO_BLOCK", OptionsConstants.NoBlock);
            desc.AddConstant("OPT_TCP_NODELAY", OptionsConstants.TcpNoDelay);
            desc.AddConstant("OPT_SOCKET_SEND_SIZE", OptionsConstants.SocketSendSize);
            desc.AddConstant("OPT_SOCKET_RECV_SIZE", OptionsConstants.SocketRecvSize);
            desc.AddConstant("OPT_CONNECT_TIMEOUT", OptionsConstants.ConnectTimeout);
            desc.AddConstant("OPT_RETRY_TIMEOUT", OptionsConstants.RetryTimeout);
            desc.AddConstant("OPT_SEND_TIMEOUT", OptionsConstants.SendTimeout);
            desc.AddConstant("OPT_RECV_TIMEOUT", OptionsConstants.RecvTimeout);
            desc.AddConstant("OPT_POLL_TIMEOUT", OptionsConstants.PollTimeout);
            desc.AddConstant("OPT_CACHE_LOOKUPS", OptionsConstants.CacheLookups);
            desc.AddConstant("OPT_SERVER_FAILURE_LIMIT", OptionsConstants.ServerFailureLimit);

            // HaveConstants
            desc.AddConstant("HAVE_IGBINARY", HaveConstants.IgBinary);
            desc.AddConstant("HAVE_JSON", HaveConstants.JSON);

            // GetConstants
            desc.AddConstant("GET_PRESERVE_ORDER", GetConstants.PreserveOrder);

            // DistributionConstants
            desc.AddConstant("DISTRIBUTION_MODULA", DistributionConstants.ModulA);
            desc.AddConstant("DISTRIBUTION_CONSISTENT", DistributionConstants.Consistent);

            // SerializerConstants
            desc.AddConstant("SERIALIZER_PHP", SerializerConstants.Php);
            desc.AddConstant("SERIALIZER_IGBINARY", SerializerConstants.IgBinary);
            desc.AddConstant("SERIALIZER_JSON", SerializerConstants.JSON);

            // HashConstants
            desc.AddConstant("HASH_DEFAULT", HashConstants.Default);
            desc.AddConstant("HASH_MD5", HashConstants.MD5);
            desc.AddConstant("HASH_CRC", HashConstants.CRC);
            desc.AddConstant("HASH_FNV1_64", HashConstants.FNV1_64);
            desc.AddConstant("HASH_FNV1A_64", HashConstants.FNV1A_32);
            desc.AddConstant("HASH_FNV1_32", HashConstants.FNV1_32);
            desc.AddConstant("HASH_FNV1A_32", HashConstants.FNV1A_32);
            desc.AddConstant("HASH_HSIEH", HashConstants.HSIEH);
            desc.AddConstant("HASH_MURMUR", HashConstants.MURMUR);

            // ResConstants
            desc.AddConstant("RES_SUCCESS", ResConstants.Success);
            desc.AddConstant("RES_FAILURE", ResConstants.Failure);
            desc.AddConstant("RES_HOST_LOOKUP_FAILURE", ResConstants.HostLookupFailure);
            desc.AddConstant("RES_UNKNOWN_READ_FAILURE", ResConstants.UnknownReadFalure);
            desc.AddConstant("RES_PROTOCOL_ERROR", ResConstants.ProtocolError);
            desc.AddConstant("RES_CLIENT_ERROR", ResConstants.ClientError);
            desc.AddConstant("RES_SERVER_ERROR", ResConstants.ServerError);
            desc.AddConstant("RES_WRITE_FAILURE", ResConstants.WriteFailure);
            desc.AddConstant("RES_DATA_EXISTS", ResConstants.DataExists);
            desc.AddConstant("RES_NOTSTORED", ResConstants.NotStored);
            desc.AddConstant("RES_NOTFOUND", ResConstants.NotFound);
            desc.AddConstant("RES_PARTIAL_READ", ResConstants.PartialRead);
            desc.AddConstant("RES_SOME_ERRORS", ResConstants.SomeErrors);
            desc.AddConstant("RES_NO_SERVERS", ResConstants.NoServers);
            desc.AddConstant("RES_END", ResConstants.End);
            desc.AddConstant("RES_ERRNO", ResConstants.ErrNo);
            desc.AddConstant("RES_BUFFERED", ResConstants.Buffered);
            desc.AddConstant("RES_TIMEOUT", ResConstants.Timeout);
            desc.AddConstant("RES_BAD_KEY_PROVIDED", ResConstants.BadKeyProvided);
            desc.AddConstant("RES_CONNECTION_SOCKET_CREATE_FAILURE", ResConstants.ConnectionSocketCreateFailure);
            desc.AddConstant("RES_PAYLOAD_FAILURE", ResConstants.PayloadFailure);
            */
        }

        #endregion
    }
}
