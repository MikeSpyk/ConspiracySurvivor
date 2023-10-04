using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public abstract class ProceduralGenThreadingBase 
{
	private Thread m_mainThread = null;
	private long m_LOCKED_isDone = 0;
	public bool isDone
	{
		get
		{
			if(Interlocked.Read(ref m_LOCKED_isDone) == 0)
			{
				return false;
			}
			else if(Interlocked.Read(ref m_LOCKED_isDone) == 1)
			{
				return true;
			}
			else
			{
				Debug.LogError("ProceduralGenThreadingBase: LOCKED_isDone out of bounds.");
				return false;
			}
		}
	}

	/// <summary>
	/// it is more dangerous to directly access m_result but faster. use result for safer access
	/// </summary>
	public float[,] m_result;
	public float[,] result
	{
		get
		{
			if(Interlocked.Read(ref m_LOCKED_isDone) == 1)
			{
				return m_result;
			}
			else
			{
				Debug.LogError("mountainInstance: attempt to access result before thread was done !");
				return null;
			}
		}
	}

	public void start()
	{
		if(m_mainThread == null)
		{

			// no multithreading start
		    //mainThreadProcedure();
			//return;
			// no multithreading end

			ThreadStart main_Threadstart = new ThreadStart(mainThreadProcedure);
			m_mainThread = new Thread(main_Threadstart);
			m_mainThread.Start();
		}
		else
		{
			Debug.LogWarning("ProceduralGenThreadingBase: attempt to start the procedure more than one time !");
		}
	}

	public virtual void dispose()
	{
		if(m_mainThread != null)
		{
			m_mainThread.Abort();
			m_mainThread = null;
		}
		m_result = null;
	}

	protected void setIsDoneState(bool newState)
	{
		if(newState == true)
		{
			if(isDone == true)
			{
				Debug.LogWarning("ProceduralGenThreadingBase: attempt to set the isDone-State to true while it is already true !");
			}
			else
			{
				Interlocked.Increment(ref m_LOCKED_isDone);
			}
		}
		else // == false
		{
			if(isDone == false)
			{
				Debug.LogWarning("ProceduralGenThreadingBase: attempt to set the isDone-State to false while it is already false !");
			}
			else
			{
				Interlocked.Decrement(ref m_LOCKED_isDone);
			}
		}
	}

	protected virtual void mainThreadProcedure(){}

}
