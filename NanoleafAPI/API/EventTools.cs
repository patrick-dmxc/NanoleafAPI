using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace NanoleafAPI
{
    public static class EventTools
    {
        private static readonly ILogger log = Tools.LoggerFactory.CreateLogger(typeof(EventTools));

        [DebuggerHidden]
        public static int InvokeFailSafe(this EventHandler @event, object sender, EventArgs args, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(sender, args), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <param name="args"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static int InvokeFailSafe(this Action @event, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <param name="args"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static int InvokeFailSafe<T>(this Action<T> @event, T args, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(args), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static int InvokeFailSafe<T>(this EventHandler<T> @event, object sender, T args, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(sender, args), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static int InvokeFailSafe(this PropertyChangedEventHandler @event, object sender, PropertyChangedEventArgs args, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(sender, args), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static int InvokeFailSafe(this CollectionChangeEventHandler @event, object sender, CollectionChangeEventArgs args, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(sender, args), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static int InvokeFailSafe(this NotifyCollectionChangedEventHandler @event, object sender, NotifyCollectionChangedEventArgs args, ILogger log = null)
        {
            return InvokeFailSaveGeneric(@event, a => a(sender, args), log);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <param name="delegate"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static IReadOnlyList<object> InvokeFailSafe(this Delegate @delegate, params object[] values)
        {
            return InvokeFailSafe(@delegate, elog: null, values: values);
        }

        /// <summary>
        /// Calles the Invoke in a safe form, where even if one of the Subscribed EventHandlers
        /// fails with an exception, the other subscribed methods still get called.
        /// </summary>
        /// <param name="delegate"></param>
        /// <param name="elog"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public static IReadOnlyList<object> InvokeFailSafe(this Delegate @delegate, ILogger elog = null, params object[] values)
        {
            return InvokeFailSaveGeneric(@delegate, a => a.DynamicInvoke(values), elog);
        }

        [DebuggerHidden]
        public static int InvokeFailSaveGeneric<TDelegate>(TDelegate @delegate, Action<TDelegate> invoker, ILogger elog = null) where TDelegate : Delegate
        {
            if (invoker == null) throw new ArgumentNullException(nameof(invoker));
            if (@delegate == null) return 0;

            var target = @delegate.GetInvocationList();
            int ret = 0;
            for (int i = 0; i < target.Length; i++)
            {
                TDelegate del = (TDelegate)target[i];
                if (del == null) continue;

                try
                {
                    invoker(del);
                    ret++;
                }
                catch (Exception e)
                {
                    (elog ?? log).LogWarning("Exception in Delegate Invocation: {0} => {1}.{2}", e, @delegate.Method, del.Target, del.Method);
                }
            }
            return ret;
        }

        [DebuggerHidden]
        public static IReadOnlyList<TReturn> InvokeFailSaveGeneric<TDelegate, TReturn>(TDelegate @delegate, Func<TDelegate, TReturn> invoker, ILogger elog = null) where TDelegate : Delegate
        {
            if (invoker == null) throw new ArgumentNullException(nameof(invoker));
            if (@delegate == null) return null;

            var target = @delegate.GetInvocationList();
            var ret = new List<TReturn>(target.Length);
            for (int i = 0; i < target.Length; i++)
            {
                TDelegate del = (TDelegate)target[i];
                if (del == null) continue;

                try
                {
                    var x = invoker(del);
                    ret.Add(x);
                }
                catch (Exception e)
                {
                    (elog ?? log).LogWarning("Exception in Delegate Invocation: {0} => {1}.{2}", e, @delegate.Method, del.Target, del.Method);
                }
            }
            return ret;
        }


    }
}
