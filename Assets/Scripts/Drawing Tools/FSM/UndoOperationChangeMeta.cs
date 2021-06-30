
using System;

class UndoOperationChangeMeta : UndoOperation
{
	private Entity m_Target;
	private string m_TargetMetaDataField;
	private string m_OldValue;
	private string m_NewValue;

	public UndoOperationChangeMeta(Entity target, string metaDataField, string oldValue, string newValue)
	{
		m_Target = target;
		m_TargetMetaDataField = metaDataField;
		m_OldValue = oldValue;
		m_NewValue = newValue;
	}

	public override void Undo(FSM fsm, out UndoOperation redo, bool totalUndo = false)
	{
		m_Target.SetMetaData(m_TargetMetaDataField, m_OldValue);
		m_Target.RedrawGameObjects(CameraManager.Instance.gameCamera);

		redo = new UndoOperationChangeMeta(m_Target, m_TargetMetaDataField, m_NewValue, m_OldValue);
	}
}

