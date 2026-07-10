using System;
using System.Collections.Generic;

namespace StorageNetwork.LogicDiy.Web
{
    /// <summary>Thread-safe ownership boundary between HTTP requests and Unity's main thread.</summary>
    internal sealed class LogicDiyEditorSessionRegistry<TLogic, TState, TSave>
        where TLogic : class
        where TState : class
        where TSave : class
    {
        private sealed class Session
        {
            public WeakReference<TLogic> Logic;
            public TState State;
            public TSave PendingSave;
            public System.DateTime LastSeenUtc;
        }

        private readonly object gate = new object();
        private readonly Dictionary<int, Session> sessions = new Dictionary<int, Session>();

        public void Register(int id, TLogic logic, TState state)
        {
            lock (gate)
            {
                sessions[id] = new Session { Logic = new WeakReference<TLogic>(logic), State = state, LastSeenUtc = System.DateTime.UtcNow };
            }
        }

        public bool TryGetState(int id, out TState state)
        {
            lock (gate)
            {
                if (sessions.TryGetValue(id, out Session session))
                {
                    state = session.State;
                    return state != null;
                }
            }

            state = null;
            return false;
        }

        public void SetState(int id, TState state)
        {
            lock (gate)
            {
                if (sessions.TryGetValue(id, out Session session)) session.State = state;
            }
        }

        public void MarkSeen(int id)
        {
            lock (gate)
            {
                if (sessions.TryGetValue(id, out Session session)) session.LastSeenUtc = System.DateTime.UtcNow;
            }
        }

        public bool IsActive(int id, TimeSpan timeout)
        {
            lock (gate)
            {
                return sessions.TryGetValue(id, out Session session) && System.DateTime.UtcNow - session.LastSeenUtc <= timeout;
            }
        }

        public bool QueueSave(int id, TSave request)
        {
            lock (gate)
            {
                if (!sessions.TryGetValue(id, out Session session)) return false;
                session.PendingSave = request;
                session.LastSeenUtc = System.DateTime.UtcNow;
                return true;
            }
        }

        public TSave TakeSave(int id)
        {
            lock (gate)
            {
                if (!sessions.TryGetValue(id, out Session session)) return null;
                TSave result = session.PendingSave;
                session.PendingSave = null;
                return result;
            }
        }

        public void Prune(TimeSpan retention)
        {
            lock (gate)
            {
                List<int> stale = new List<int>();
                foreach (KeyValuePair<int, Session> pair in sessions)
                {
                    if (System.DateTime.UtcNow - pair.Value.LastSeenUtc > retention || !pair.Value.Logic.TryGetTarget(out TLogic _)) stale.Add(pair.Key);
                }
                foreach (int id in stale) sessions.Remove(id);
            }
        }
    }
}
