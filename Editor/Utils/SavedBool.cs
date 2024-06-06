using UnityEditor;


namespace CoderScripts.MeshEditorPlus
{
    internal class SavedBool
    {
        private bool m_Value;

        private string m_Name;

        private bool m_Loaded;

        public bool value
        {
            get
            {
                Load();
                return m_Value;
            }
            set
            {
                Load();
                if (m_Value != value)
                {
                    m_Value = value;
                    EditorPrefs.SetBool(m_Name, value);
                }
            }
        }

        public SavedBool(string name, bool value)
        {
            m_Name = name;
            m_Loaded = false;
            m_Value = value;
        }

        private void Load()
        {
            if (!m_Loaded)
            {
                m_Loaded = true;
                m_Value = EditorPrefs.GetBool(m_Name, m_Value);
            }
        }

        public static implicit operator bool(SavedBool s)
        {
            return s.value;
        }
    }

}
