using System;
using System.Collections.Generic;
using System.Linq;

namespace Promises
{
    enum PromiseState
    {
        Pending,
        Resolved,
        Rejected
    }

    enum ContextType
    {
        Then,
        Catch,
        Finally
    }

    public class PromiseSetupException : Exception
    {
        public PromiseSetupException() : base() { }
        public PromiseSetupException(string message) : base(message) { }
    }

    public class PromiseRejectUnhandledException : Exception
    {
        public PromiseRejectUnhandledException() : base() { }
        public PromiseRejectUnhandledException(string message, Exception ex) : base(message, ex) { }
    }


    public delegate void Resolver(object value);
    public delegate void Rejecter(Exception ex);
    public delegate object ResolveHandler(object value);
    public delegate object RejectHandler(Exception ex);
    public delegate void Executor<Type>(Action<Type> resolve, Action<Exception> reject);

    interface IPromise
    {
        IPromise Then(object value);
    }

    public struct DeferredState
    {
        public readonly object Value;
        public readonly Exception Reason;

        public DeferredState(object value)
        {
            Value = value;
            Reason = null;
        }

        public DeferredState(Exception reason)
        {
            Value = null;
            Reason = reason;
        }
    }

    public class Promise: IPromise
    {
        private PromiseState _state = PromiseState.Pending;
        private object _fulfilledValue = null;
        private Exception _rejectedReason = null;
        private bool _canSettle = false;

        private class DeferredContext
        {
            public ContextType ContextType;
            public ResolveHandler Fulfill;
            public RejectHandler Reject;
        }

        private List<DeferredContext> _chain = new List<Promise.DeferredContext>();

        private void _Resolve(Type value)
        {
            _state = PromiseState.Resolved;
            _fulfilledValue = value;

            if (_canSettle)
            {
                _Settle();
            }

        }

        private void _Reject(Exception ex)
        {
            _state = PromiseState.Rejected;
            _rejectedReason = ex;

            if (_canSettle)
            {
                _Settle();
            }
        }

        private DeferredState _Reducer(DeferredState state, DeferredContext context)
        {
            Func<object> action = null;
            switch (context.ContextType)
            {
                case ContextType.Then:
                    if (state.Reason != null)
                    {
                        if (context.Reject != null)
                        {
                            action = () => context.Reject(state.Reason);
                        }
                    }
                    else if (context.Fulfill != null)
                    {
                        action = () => context.Fulfill(state.Value);
                    }
                    break;

                case ContextType.Catch:
                    if (state.Reason != null)
                    {
                        action = () => context.Reject(state.Reason);
                    }
                    break;

                case ContextType.Finally:
                    action = () => context.Fulfill(state.Value);
                    break;
            }

            if (action == null)
            {
                return state;
            }

            object result = null;
            try
            {
                result = action();
            } catch (Exception ex)
            {
                return new DeferredState(ex);
            }

            if (result is Promise)
            {
                Promise p = result as Promise;

            }
        }

        private (object, Exception) _ReduceChain((object, Exception) seed)
        {
            var result = seed;
            _chain.ForEach(context =>
            {
                result = reduceFunc(result, context);
            });

            return result;
        }

        private void _Settle()
        {
            var state = (_fulfilledValue, _rejectedReason);
            _chain.Aggregate(state, (onResolve, onReject, )

            if (_chain.Count < 1)
            {
                return;
            }

            var index = 0;
            Func<object> handler = null;
            if (_state == PromiseState.Rejected)
            {
                var context = _chain[0];
                if (context.Reject != null)
                {
                    handler = () => context.Reject;
                }
                else
                {
                    index = _chain.FindIndex(c => c.ContextType == ContextType.Catch);
                    if (index == -1)
                    {
                        throw new PromiseRejectUnhandledException("Unhandled rejected promise", _rejectedReason);
                    }

                    handler = () => catchContext.Reject;
                }
            }
            if (_state == PromiseState.Resolved)
            {

            }
        }

        private bool IsSettled { get { return _state != PromiseState.Pending; } }

        public Promise(Executor<Type> executor)
        {
            try
            {
                executor(_Resolve, _Reject);
            }
            catch (Exception ex)
            {
                _Reject(ex);
            }

            _canSettle = true;
        }

        public Promise Then(ResolveHandler<Type> onFulfilled = null, RejectHandler onRejected = null)
        {
            _chain.Add(new DeferredContext
            {
                ContextType = ContextType.Then,
                Fulfill = onFulfilled,
                Reject = onRejected
            });

            return this;
        }

        public Promise Catch(RejectHandler rejectFunc)
        {
            if (rejectFunc == null)
            {
                throw new ArgumentNullException("rejectFunc");
            }

            _chain.Add(new DeferredContext
            {
                ContextType = ContextType.Catch,
                Reject = rejectFunc
            });

            return this;
        }

        public Promise Finally(ResolveHandler<Type> resolveFunc)
        {
            if (resolveFunc == null)
            {
                throw new ArgumentNullException("resolveFunc");
            }

            if (_chain.Count < 1 || _chain.Last().ContextType != ContextType.Catch)
            {
                throw new PromiseSetupException("A call to Promise.Finally() must occurr immediately following a call to Promise.Catch()");
            }

            _chain.Add(new DeferredContext
            {
                ContextType = ContextType.Finally,
                Fulfill = resolveFunc,
            });

            return this;
        }
    }
}
