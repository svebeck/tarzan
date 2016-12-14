using System.Collections;

public class ThreadedJob
{
    private bool m_IsDone = false;
    private object m_Handle = new object();

    #if !UNITY_WEBGL
    private System.Threading.Thread m_Thread = null;
    #endif

    public bool IsDone
    {
        get
        {
            bool tmp;
            #if !UNITY_WEBGL
            lock (m_Handle)
            {
                tmp = m_IsDone;
            }
            #else
            tmp = m_IsDone;
            #endif
            return tmp;
        }
        set
            {
            #if !UNITY_WEBGL
            lock (m_Handle)
            {
                m_IsDone = value;
            }
            #else
            m_IsDone = value;
            #endif
        }
    }

    public virtual void Start()
    {
        m_IsDone = false;
        #if UNITY_WEBGL
        Run();
        #else
        m_Thread = new System.Threading.Thread(Run);
        m_Thread.Start();
        #endif
    }
    public virtual void Abort()
    {
        #if !UNITY_WEBGL
        m_Thread.Abort();
        #endif
    }

    protected virtual void ThreadFunction() { }

    protected virtual void OnFinished() { }

    public virtual bool Update()
    {
        if (IsDone)
        {
            OnFinished();
            return true;
        }
        return false;
    }
    public IEnumerator WaitFor()
    {
        while(!Update())
        {
            yield return null;
        }
    }
    private void Run()
    {
        ThreadFunction();
        IsDone = true;
    }
}