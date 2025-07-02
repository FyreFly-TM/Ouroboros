import React, { useEffect } from 'react';
import Prism from 'prismjs';
import 'prismjs/themes/prism.css';
import '../assets/styles/prism-theme.css';

// Define Ouroboros language for Prism.js
Prism.languages.ouroboros = {
  'comment': [
    {
      pattern: /\/\/.*/,
      greedy: true
    },
    {
      pattern: /\/\*[\s\S]*?\*\//,
      greedy: true
    }
  ],
  'string': {
    pattern: /"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'/,
    greedy: true
  },
  'syntax-level': {
    pattern: /@(high|medium|low)\b/,
    alias: 'keyword'
  },
  'keyword': /\b(?:def|fun|fn|function|if|else|elif|while|for|return|break|continue|match|with|as|class|struct|enum|interface|trait|impl|self|this|new|delete|null|nil|true|false|let|const|var|mut|public|private|protected|static|async|await|yield|import|export|from|package|module|type|typeof|instanceof|is|in|not|and|or|xor|throw|try|catch|finally|unsafe|where|repeat|times|until|unless|loop|forever|then|repeat|with|probability|choose|between|from|list|select|when|end|begin|lambda|macro|template|generic|concept|requires|namespace|using|inline|virtual|override|abstract|explicit|implicit|operator|friend|typedef|alias|extension|annotation|attribute|decorator|meta|reflect|compile|eval|quote|unquote|splice|transform|rewrite|syntax|rule|pattern|lexer|parser|ast|visitor|transformer|codegen|emit|optimize|link|debug|profile|trace|assert|test|benchmark|measure|log|print|error|warning|info|verbose|quiet|silent|config|option|setting|parameter|argument|result|value|property|field|method|event|signal|slot|delegate|handler|callback|listener|observer|subscribe|publish|notify|broadcast|send|receive|request|response|promise|future|task|job|worker|thread|process|channel|pipe|stream|buffer|queue|stack|heap|pool|cache|store|database|table|row|column|index|key|lock|mutex|semaphore|condition|barrier|latch|atomic|volatile|synchronized|concurrent|parallel|distributed|cluster|node|network|socket|connection|session|transaction|commit|rollback|savepoint|checkpoint|snapshot|backup|restore|migrate|seed|validate|sanitize|escape|encode|decode|encrypt|decrypt|hash|sign|verify|authenticate|authorize|permission|role|user|group|policy|rule|grant|revoke|deny|allow|audit|log|monitor|alert|metric|gauge|counter|histogram|timer|trace|span|context|correlation|cause|effect|trigger|action|reaction|behavior|state|transition|machine|automaton|graph|tree|node|edge|vertex|path|cycle|component|subgraph|traversal|search|sort|filter|map|reduce|fold|scan|zip|unzip|flatten|group|partition|join|merge|split|slice|splice|insert|remove|replace|update|patch|diff|delta|change|revision|version|tag|branch|fork|merge|rebase|cherry|pick|stash|reset|revert|undo|redo|history|timeline|snapshot|checkpoint|milestone|release|deploy|publish|distribute|install|uninstall|upgrade|downgrade|configure|build|compile|link|run|execute|launch|start|stop|pause|resume|restart|reload|refresh|sync|async|defer|delay|timeout|interval|schedule|cron|timer|clock|timestamp|datetime|duration|period|range|span|window|frame|batch|chunk|page|limit|offset|cursor|iterator|generator|enumerator|sequence|series|list|array|vector|matrix|tensor|scalar|tuple|set|bag|multiset|map|dictionary|hashmap|treemap|graph|tree|heap|trie|bloom|sketch|probabilistic|approximate|exact|deterministic|random|stochastic|heuristic|greedy|dynamic|recursive|iterative|incremental|differential|integral|derivative|gradient|jacobian|hessian|laplacian|fourier|wavelet|convolution|correlation|regression|classification|clustering|dimensionality|reduction|feature|extraction|selection|engineering|preprocessing|normalization|standardization|scaling|transformation|projection|embedding|encoding|decoding|compression|decompression|serialization|deserialization|marshalling|unmarshalling|pickling|unpickling|parsing|formatting|pretty|printing|rendering|layout|styling|theming|templating|binding|routing|navigation|middleware|interceptor|filter|guard|validator|sanitizer|converter|adapter|wrapper|proxy|facade|bridge|composite|decorator|strategy|observer|command|memento|visitor|iterator|factory|builder|prototype|singleton|pool|registry|repository|service|controller|presenter|view|model|component|element|widget|control|panel|dialog|modal|popup|tooltip|menu|toolbar|statusbar|sidebar|header|footer|content|container|wrapper|section|article|aside|nav|main|div|span|paragraph|heading|title|subtitle|caption|label|text|image|icon|button|link|input|output|form|field|checkbox|radio|select|option|dropdown|combobox|listbox|textarea|editor|viewer|player|slider|progress|spinner|loader|indicator|badge|tag|chip|card|tile|grid|flex|table|row|column|cell|header|footer|body|thead|tbody|tfoot|tr|td|th|caption|col|colgroup|summary|details|figure|figcaption|picture|source|video|audio|canvas|svg|path|circle|rect|ellipse|line|polyline|polygon|text|tspan|defs|symbol|use|g|mask|pattern|filter|gradient|stop|animate|animateTransform|set|marker|clipPath|metadata|title|desc|script|style|link|meta|base|head|body|html|document|window|screen|navigator|location|history|console|math|json|regexp|error|promise|proxy|reflect|symbol|iterator|generator|async|await|yield)\b/,
  'builtin': /\b(?:print|println|input|len|range|enumerate|zip|map|filter|reduce|sum|min|max|abs|round|floor|ceil|sqrt|pow|sin|cos|tan|asin|acos|atan|atan2|log|log10|exp|random|randint|uniform|normal|choice|shuffle|sort|sorted|reverse|reversed|join|split|strip|replace|upper|lower|capitalize|title|startswith|endswith|find|index|count|format|parse|int|float|str|bool|list|dict|set|tuple|type|isinstance|issubclass|hasattr|getattr|setattr|delattr|dir|help|id|hash|bin|hex|oct|ord|chr|bytes|bytearray|memoryview|open|close|read|write|seek|tell|flush|exists|isfile|isdir|mkdir|rmdir|remove|rename|copy|move|walk|glob|re|match|search|findall|finditer|sub|subn|compile|escape|purge|error|datetime|date|time|timedelta|timezone|now|today|utcnow|fromtimestamp|strftime|strptime|sleep|clock|perf_counter|process_time|Timer|Lock|RLock|Semaphore|Event|Condition|Barrier|Queue|PriorityQueue|LifoQueue|SimpleQueue|Thread|Process|Pool|Executor|Future|asyncio|run|create_task|gather|wait|wait_for|shield|timeout|sleep|Task|create_subprocess_exec|create_subprocess_shell|subprocess|Popen|PIPE|STDOUT|DEVNULL|CalledProcessError|TimeoutExpired|socket|AF_INET|AF_INET6|SOCK_STREAM|SOCK_DGRAM|SOL_SOCKET|SO_REUSEADDR|gethostname|gethostbyname|getaddrinfo|getfqdn|create_connection|create_server|socketpair|fromfd|SocketType|SocketKind|SocketIO|urllib|request|urlopen|urlretrieve|urlparse|urljoin|urlencode|quote|unquote|http|HTTPStatus|HTTPConnection|HTTPSConnection|HTTPException|NotConnected|InvalidURL|UnknownProtocol|UnknownTransferEncoding|UnimplementedFileMode|IncompleteRead|ImproperConnectionState|CannotSendRequest|CannotSendHeader|ResponseNotReady|BadStatusLine|LineTooLong|RemoteDisconnected|json|loads|dumps|load|dump|JSONEncoder|JSONDecoder|JSONDecodeError|xml|etree|ElementTree|parse|Element|SubElement|tostring|fromstring|iterparse|XMLParser|XMLPullParser|ParseError|csv|reader|writer|DictReader|DictWriter|excel|excel_tab|field_size_limit|get_dialect|list_dialects|register_dialect|unregister_dialect|Sniffer|SnifferError|Dialect|QuoteStyle|sqlite3|connect|Connection|Cursor|Row|IntegrityError|OperationalError|ProgrammingError|DataError|NotSupportedError|pickle|dumps|loads|dump|load|Pickler|Unpickler|PickleError|PicklingError|UnpicklingError|hashlib|md5|sha1|sha224|sha256|sha384|sha512|blake2b|blake2s|pbkdf2_hmac|scrypt|new|algorithms_guaranteed|algorithms_available|hmac|new|compare_digest|HMAC|secrets|token_bytes|token_hex|token_urlsafe|choice|randbelow|randbits|SystemRandom|compare_digest|uuid|UUID|uuid1|uuid3|uuid4|uuid5|NAMESPACE_DNS|NAMESPACE_URL|NAMESPACE_OID|NAMESPACE_X500|getnode|os|path|exists|isfile|isdir|join|split|splitext|basename|dirname|abspath|relpath|normpath|realpath|expanduser|expandvars|getcwd|chdir|listdir|mkdir|makedirs|rmdir|remove|unlink|rename|replace|stat|lstat|getsize|getmtime|getatime|getctime|walk|scandir|DirEntry|environ|getenv|putenv|unsetenv|system|popen|fdopen|close|closerange|device_encoding|dup|dup2|fchdir|fchmod|fchown|fdatasync|fpathconf|fstat|fstatvfs|fsync|ftruncate|get_terminal_size|isatty|lockf|lseek|open|openpty|pipe|pipe2|posix_fadvise|posix_fallocate|pread|preadv|pwrite|pwritev|read|readv|sendfile|set_blocking|get_blocking|set_inheritable|get_inheritable|readlink|symlink|sync|truncate|ttyname|unlink|utime|write|writev|sys|argv|path|modules|stdin|stdout|stderr|version|version_info|platform|exit|getdefaultencoding|getfilesystemencoding|getrecursionlimit|setrecursionlimit|gettrace|settrace|getprofile|setprofile|getsizeof|getrefcount|getframe|exc_info|excepthook|__import__|reload|iter|next|StopIteration|GeneratorExit|KeyboardInterrupt|SystemExit|Exception|BaseException|ArithmeticError|AssertionError|AttributeError|BufferError|EOFError|ImportError|LookupError|MemoryError|NameError|NotImplementedError|OSError|OverflowError|RecursionError|ReferenceError|RuntimeError|StopAsyncIteration|SyntaxError|SystemError|TypeError|UnboundLocalError|UnicodeError|UnicodeDecodeError|UnicodeEncodeError|UnicodeTranslateError|ValueError|ZeroDivisionError|BlockingIOError|BrokenPipeError|ChildProcessError|ConnectionError|ConnectionAbortedError|ConnectionRefusedError|ConnectionResetError|FileExistsError|FileNotFoundError|InterruptedError|IsADirectoryError|NotADirectoryError|PermissionError|ProcessLookupError|TimeoutError|IndentationError|IndexError|KeyError|ModuleNotFoundError|TabError|UnicodeWarning|BytesWarning|ResourceWarning)\b/,
  'greek-symbol': /\b(?:π|τ|φ|∞|∑|∏|√|∂|∇|∈|∉|∪|∩|⊂|⊃|⊆|⊇|∅|∀|∃|λ|α|β|γ|δ|ε|ζ|η|θ|ι|κ|μ|ν|ξ|ο|ρ|σ|υ|χ|ψ|ω|Γ|Δ|Θ|Λ|Ξ|Π|Σ|Φ|Ψ|Ω)\b/,
  'math-symbol': /[+\-*\/%^&|~<>=!]+|::|->|=>|\.\.\.?|\?[.?]|[{}[\];(),.:`]/,
  'number': [
    // Complex numbers
    /\b\d+(?:\.\d+)?[+-]\d+(?:\.\d+)?[ij]\b/,
    // Scientific notation
    /\b\d+(?:\.\d+)?[eE][+-]?\d+\b/,
    // Decimals
    /\b\d+\.\d+\b/,
    // Integers (including hex, oct, bin)
    /\b0[xX][\da-fA-F]+\b/,
    /\b0[oO][0-7]+\b/,
    /\b0[bB][01]+\b/,
    /\b\d+\b/
  ],
  'boolean': /\b(?:true|false|True|False|yes|no|on|off)\b/,
  'operator': /[+\-*\/%^&|~<>=!]=?|::|->|=>|\.\.\.?|\?[.?]/,
  'punctuation': /[{}[\];(),.:`]/,
  'variable': /\$\w+|\b[A-Z_][A-Z0-9_]*\b/,
  'function': /\b\w+(?=\s*\()/,
  'class-name': /\b[A-Z]\w*\b/
};

const CodeBlock = ({ children, code, language = 'ouroboros', showLineNumbers = false }) => {
  const codeRef = React.useRef(null);

  // Get the code content from either prop
  const codeString = code || (typeof children === 'string' 
    ? children 
    : Array.isArray(children) 
      ? children.join('') 
      : String(children || ''));

  useEffect(() => {
    if (codeRef.current && codeString) {
      try {
        let highlighted;
        if (Prism.languages[language]) {
          highlighted = Prism.highlight(codeString, Prism.languages[language], language);
        } else {
          // Fallback: just escape HTML
          highlighted = codeString
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
        }
        
        codeRef.current.innerHTML = highlighted;
      } catch (e) {
        console.warn('Prism highlighting failed:', e);
        // Fallback to plain text
        codeRef.current.textContent = codeString;
      }
    }
  }, [codeString, language]);

  if (!codeString) {
    return null;
  }

  return (
    <div className="relative group mb-6">
      <pre className={`language-${language} ${showLineNumbers ? 'line-numbers' : ''} overflow-x-auto rounded-lg border border-gray-300 dark:border-gray-700 bg-gray-100 dark:bg-gray-800 p-4`}>
        <code 
          ref={codeRef}
          className={`language-${language} text-gray-900 dark:text-gray-100`}
          style={{ display: 'block', whiteSpace: 'pre', wordWrap: 'normal' }}
        >
          {codeString}
        </code>
      </pre>
      <button
        onClick={() => {
          navigator.clipboard.writeText(codeString).then(() => {
            // You could add a toast notification here
            console.log('Code copied to clipboard');
          }).catch(err => {
            console.error('Failed to copy code:', err);
          });
        }}
        className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity bg-gray-700 hover:bg-gray-600 text-white px-2 py-1 rounded text-sm"
        title="Copy code"
      >
        Copy
      </button>
    </div>
  );
};

export default CodeBlock; 