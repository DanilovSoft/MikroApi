using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DanilovSoft.MikroApi
{
    [DebuggerDisplay("{DebugDisplay,nq}")]
    [DebuggerTypeProxy(typeof(TypeProxy))]
    public class MikroTikCommand
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay
        {
            get
            {
                if (_tagIndex == -1)
                {
                    return $"\"{_lines[0]}\"";
                }
                else
                {
                    return $"\"{_lines[0]}\", {_lines[_tagIndex]}";
                }
            }
        }
        internal readonly List<string> _lines = new();
        private int _tagIndex = -1;
        /// <summary>
        /// True если команда была успешно отправлена. Отправленные команды нельзя отправлять повторно.
        /// </summary>
        public bool IsCompleted { get; private set; }

        // ctor
        public MikroTikCommand(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            string formatted = string.Join("/", command.Split(' '));
            AddLine(formatted);
        }

        /// <summary>
        /// Добавляет строку в список без каких либо проверок и изменений.
        /// </summary>
        internal void AddLine(string text)
        {
            _lines.Add(text);
        }

        internal void AddQuery(string queryText)
        {
            AddLine(queryText);
        }

        internal void AddAttribute(string attributeText)
        {
            if (attributeText.StartsWith("=.tag=", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Нельзя самостоятельно устанавливать тег");
            }

            AddLine(attributeText);
        }

        /// <summary>
        /// Устанавливает строку ".tag={tag}"
        /// </summary>
        internal MikroTikCommand SetTag(string tag)
        {
            if (_tagIndex == -1)
            {
                _tagIndex = _lines.Count;
                _lines.Add($".tag={tag}");
            }
            else
            {
                _lines[_tagIndex] = $".tag={tag}";
            }
            return this;
        }

        #region Query

        /// <summary>
        /// Добавляет уточнение вида "?query"
        /// </summary>
        public MikroTikCommand Query(string query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            AddQuery($"?{query}");
            return this;
        }

        /// <summary>
        /// Добавляет уточнение вида "?query=value"
        /// </summary>
        public MikroTikCommand Query(string query, string value)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            AddQuery($"?{query}={value}");
            return this;
        }

        /// <summary>
        /// Добавляет уточнение вида "?query=value1,value2,..."
        /// </summary>
        public MikroTikCommand Query(string query, params string[] values)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            string joined = string.Join(",", values);
            AddQuery($"?{query}={joined}");
            return this;
        }

        /// <summary>
        /// Добавляет уточнение вида "?query=value1,value2,..."
        /// </summary>
        public MikroTikCommand Query(string query, IEnumerable<string> values)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            string joined = string.Join(",", values);
            AddQuery($"?{query}={joined}");
            return this;
        }

        #endregion

        #region Attribute

        /// <summary>
        /// Добавляет атрибут вида "=name="
        /// </summary>
        public MikroTikCommand Attribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddAttribute($"={name}=");
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=name=value"
        /// </summary>
        public MikroTikCommand Attribute(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddAttribute($"={name}={value}");
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=name=value1,value2,..."
        /// </summary>
        public MikroTikCommand Attribute(string name, params string[] values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            string joined = string.Join(",", values);
            AddAttribute($"={name}={joined}");
            return this;
        }

        /// <summary>
        /// Добавляет атрибут вида "=name=value1,value2,..."
        /// </summary>
        public MikroTikCommand Attribute(string name, IEnumerable<string> values)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            string joined = string.Join(",", values);
            AddAttribute($"={name}={joined}");
            return this;
        }

        #endregion

        #region Proplist

        /// <summary>
        /// Добавляет атрибут вида "=.proplist=value1,value2,..."
        /// </summary>
        public MikroTikCommand Proplist(IEnumerable<string> values)
        {
            return Attribute(".proplist", values);
        }

        /// <summary>
        /// Добавляет атрибут вида "=.proplist=value1,value2,..."
        /// </summary>
        public MikroTikCommand Proplist(params string[] values)
        {
            return Attribute(".proplist", values);
        }

        #endregion

        /// <summary>
        /// Запрещает использовать эту команду повторно.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Completed()
        {
            // Нельзя использовать эту команду повторно.
            IsCompleted = true;
        }

        /// <summary>
        /// Генерирует исключение если текущий экземпляр команды уже был однажды отправлен в сокет.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfCompleted()
        {
            if (IsCompleted)
            {
                throw new InvalidOperationException("This command is already sent");
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _lines.Count; i++)
            {
                sb.AppendLine(_lines[i]);
            }
            return sb.ToString();
        }

        [DebuggerNonUserCode]
        private class TypeProxy
        {
            private readonly MikroTikCommand _self;
            public TypeProxy(MikroTikCommand self)
            {
                _self = self;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public string[] Items => _self._lines.Skip(1).ToArray();
        }
    }
}
