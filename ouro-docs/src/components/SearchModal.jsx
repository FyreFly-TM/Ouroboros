import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Search, X } from 'lucide-react'

const searchableContent = [
  // Math Symbols
  { title: 'π (Pi)', path: '/math-symbols#pi', content: 'Mathematical constant pi ≈ 3.14159' },
  { title: 'τ (Tau)', path: '/math-symbols#tau', content: 'Mathematical constant tau = 2π' },
  { title: 'e (Euler\'s Number)', path: '/math-symbols#e', content: 'Mathematical constant e ≈ 2.71828' },
  { title: 'φ (Golden Ratio)', path: '/math-symbols#phi', content: 'Golden ratio φ ≈ 1.61803' },
  { title: '∞ (Infinity)', path: '/math-symbols#infinity', content: 'Infinity symbol and usage' },
  { title: 'Complex Numbers', path: '/math-symbols#complex', content: 'Working with complex numbers and i' },
  { title: 'Set Operations', path: '/math-symbols#sets', content: '∪ union, ∩ intersection, ∈ element of' },
  
  // High Level Syntax
  { title: 'Natural Variable Declaration', path: '/high-level-syntax#variables', content: 'name := value syntax' },
  { title: 'String Interpolation', path: '/high-level-syntax#strings', content: '${variable} in strings' },
  { title: 'repeat times', path: '/high-level-syntax#repeat', content: 'Natural loop syntax: repeat N times' },
  { title: 'Pattern Matching', path: '/high-level-syntax#match', content: 'match/when pattern matching' },
  { title: 'Natural Conditionals', path: '/high-level-syntax#if', content: 'if X is greater than Y syntax' },
  { title: 'for each in', path: '/high-level-syntax#foreach', content: 'Natural iteration syntax' },
  { title: 'print Statement', path: '/high-level-syntax#print', content: 'print "text" statement' },
  
  // Medium Level Syntax
  { title: 'Type Inference', path: '/medium-level-syntax#type-inference', content: 'var keyword and type inference' },
  { title: 'Lambda Expressions', path: '/medium-level-syntax#lambda', content: 'Arrow functions x => x * x' },
  { title: 'LINQ Operations', path: '/medium-level-syntax#linq', content: 'Where, Select, Aggregate' },
  { title: 'async/await', path: '/medium-level-syntax#async', content: 'Asynchronous programming' },
  { title: 'Exception Handling', path: '/medium-level-syntax#exceptions', content: 'try/catch/finally blocks' },
  { title: 'Pattern Matching (C# style)', path: '/medium-level-syntax#pattern', content: 'is operator and patterns' },
  
  // Low Level Syntax
  { title: 'unsafe Code', path: '/low-level-syntax#unsafe', content: 'unsafe blocks and pointers' },
  { title: 'stackalloc', path: '/low-level-syntax#stackalloc', content: 'Stack memory allocation' },
  { title: 'Pointer Arithmetic', path: '/low-level-syntax#pointers', content: 'Direct memory manipulation' },
  { title: 'Bit Manipulation', path: '/low-level-syntax#bits', content: 'Bitwise operations |, &, ^, <<, >>' },
  { title: 'SIMD Operations', path: '/low-level-syntax#simd', content: 'Vector hardware acceleration' },
  { title: 'fixed Arrays', path: '/low-level-syntax#fixed', content: 'Fixed memory arrays' },
  
  // UI Framework
  { title: 'Window', path: '/ui-framework#window', content: 'Creating application windows' },
  { title: 'MenuBar', path: '/ui-framework#menubar', content: 'Application menus' },
  { title: 'ToolBar', path: '/ui-framework#toolbar', content: 'Toolbar with buttons' },
  { title: 'TabControl', path: '/ui-framework#tabs', content: 'Tabbed interfaces' },
  { title: 'Button', path: '/ui-framework#button', content: 'Button controls' },
  { title: 'TextBox', path: '/ui-framework#textbox', content: 'Text input controls' },
  { title: 'CheckBox', path: '/ui-framework#checkbox', content: 'Checkbox controls' },
  { title: 'RadioButton', path: '/ui-framework#radio', content: 'Radio button groups' },
  { title: 'Slider', path: '/ui-framework#slider', content: 'Slider controls' },
  { title: 'ComboBox', path: '/ui-framework#combobox', content: 'Dropdown selection' },
  { title: 'DatePicker', path: '/ui-framework#datepicker', content: 'Date selection control' },
  { title: 'ColorPicker', path: '/ui-framework#colorpicker', content: 'Color selection control' },
  
  // Collections
  { title: 'List<T>', path: '/collections#list', content: 'Dynamic arrays' },
  { title: 'Dictionary<K,V>', path: '/collections#dictionary', content: 'Key-value pairs' },
  { title: 'Stack<T>', path: '/collections#stack', content: 'LIFO collection' },
  { title: 'Queue<T>', path: '/collections#queue', content: 'FIFO collection' },
  { title: 'HashSet<T>', path: '/collections#hashset', content: 'Unique element collection' },
  
  // Linear Algebra
  { title: 'Vector', path: '/linear-algebra#vector', content: 'Vector math operations' },
  { title: 'Matrix', path: '/linear-algebra#matrix', content: 'Matrix operations' },
  { title: 'Quaternion', path: '/linear-algebra#quaternion', content: 'Rotation representations' },
  { title: 'Transform', path: '/linear-algebra#transform', content: 'Position, rotation, scale' },
]

export default function SearchModal({ open, onClose }) {
  const [query, setQuery] = useState('')
  const [results, setResults] = useState([])
  const navigate = useNavigate()

  useEffect(() => {
    if (query.length > 0) {
      const filtered = searchableContent.filter(item =>
        item.title.toLowerCase().includes(query.toLowerCase()) ||
        item.content.toLowerCase().includes(query.toLowerCase())
      )
      setResults(filtered.slice(0, 10))
    } else {
      setResults([])
    }
  }, [query])

  const handleResultClick = (path) => {
    const [pathname, hash] = path.split('#')
    navigate(pathname)
    if (hash) {
      setTimeout(() => {
        const element = document.getElementById(hash)
        if (element) {
          element.scrollIntoView({ behavior: 'smooth' })
        }
      }, 100)
    }
    onClose()
    setQuery('')
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="min-h-screen px-4 text-center">
        <div className="fixed inset-0 bg-gray-900/50" onClick={onClose} />
        
        <span className="inline-block h-screen align-middle" aria-hidden="true">
          &#8203;
        </span>
        
        <div className="relative inline-block w-full max-w-2xl p-6 my-8 text-left align-middle transition-all transform bg-white dark:bg-gray-800 shadow-xl rounded-2xl">
          <div className="flex items-center mb-4">
            <Search className="h-5 w-5 text-gray-400 mr-3" />
            <input
              type="text"
              className="flex-1 bg-transparent border-0 outline-none text-gray-900 dark:text-white placeholder-gray-400"
              placeholder="Search documentation..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              autoFocus
            />
            <button
              onClick={onClose}
              className="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            >
              <X className="h-5 w-5 text-gray-400" />
            </button>
          </div>
          
          {results.length > 0 && (
            <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
              {results.map((result, index) => (
                <button
                  key={index}
                  onClick={() => handleResultClick(result.path)}
                  className="w-full text-left p-3 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg mb-2"
                >
                  <div className="font-medium text-gray-900 dark:text-white">
                    {result.title}
                  </div>
                  <div className="text-sm text-gray-500 dark:text-gray-400">
                    {result.content}
                  </div>
                </button>
              ))}
            </div>
          )}
          
          {query && results.length === 0 && (
            <div className="text-center py-8 text-gray-500 dark:text-gray-400">
              No results found for "{query}"
            </div>
          )}
        </div>
      </div>
    </div>
  )
} 