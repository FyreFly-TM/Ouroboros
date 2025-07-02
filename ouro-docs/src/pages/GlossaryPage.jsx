import React from 'react';
import CodeBlock from '../components/CodeBlock';

const GlossaryPage = () => {
  const symbols = [
    // Greek Letters - Math Constants
    {
      symbol: 'π',
      name: 'Pi',
      unicode: 'U+03C0',
      category: 'Mathematical Constant',
      value: '3.14159265358979...',
      usage: 'Circle calculations, trigonometry',
      example: 'circumference := 2 * π * radius'
    },
    {
      symbol: 'τ',
      name: 'Tau',
      unicode: 'U+03C4',
      category: 'Mathematical Constant',
      value: '6.28318530717959... (2π)',
      usage: 'Alternative circle constant',
      example: 'circumference := τ * radius'
    },
    {
      symbol: 'e',
      name: 'Euler\'s Number',
      unicode: 'U+0065',
      category: 'Mathematical Constant',
      value: '2.71828182845905...',
      usage: 'Natural logarithms, exponential growth',
      example: 'compound := principal * e^(rate * time)'
    },
    {
      symbol: 'φ',
      name: 'Phi (Golden Ratio)',
      unicode: 'U+03C6',
      category: 'Mathematical Constant',
      value: '1.61803398874989...',
      usage: 'Aesthetic proportions, Fibonacci calculations',
      example: 'golden_rectangle := width * φ'
    },
    {
      symbol: 'γ',
      name: 'Euler-Mascheroni Constant',
      unicode: 'U+03B3',
      category: 'Mathematical Constant',
      value: '0.57721566490153...',
      usage: 'Number theory, harmonic series',
      example: 'harmonic_sum := ln(n) + γ'
    },
    {
      symbol: '∞',
      name: 'Infinity',
      unicode: 'U+221E',
      category: 'Mathematical Constant',
      value: 'Positive infinity',
      usage: 'Unbounded values, limits',
      example: 'for i in 0..∞ { if condition { break } }'
    },
    
    // Physics Constants
    {
      symbol: 'c',
      name: 'Speed of Light',
      unicode: 'U+0063',
      category: 'Physics Constant',
      value: '299,792,458 m/s',
      usage: 'Relativistic calculations',
      example: 'energy := mass * c²'
    },
    {
      symbol: 'G',
      name: 'Gravitational Constant',
      unicode: 'U+0047',
      category: 'Physics Constant',
      value: '6.67430 × 10⁻¹¹ m³ kg⁻¹ s⁻²',
      usage: 'Gravitational force calculations',
      example: 'force := G * (m1 * m2) / r²'
    },
    {
      symbol: 'h',
      name: 'Planck Constant',
      unicode: 'U+0068',
      category: 'Physics Constant',
      value: '6.62607015 × 10⁻³⁴ J⋅s',
      usage: 'Quantum mechanics',
      example: 'energy := h * frequency'
    },
    {
      symbol: 'ℏ',
      name: 'Reduced Planck Constant',
      unicode: 'U+210F',
      category: 'Physics Constant',
      value: 'h / 2π',
      usage: 'Quantum mechanics, angular momentum',
      example: 'uncertainty := ℏ / 2'
    },
    {
      symbol: 'k_B',
      name: 'Boltzmann Constant',
      unicode: 'k U+0042',
      category: 'Physics Constant',
      value: '1.380649 × 10⁻²³ J/K',
      usage: 'Statistical mechanics, thermodynamics',
      example: 'avg_energy := (3/2) * k_B * T'
    },
    
    // Set Operations
    {
      symbol: '∪',
      name: 'Union',
      unicode: 'U+222A',
      category: 'Set Operation',
      value: 'Set union operator',
      usage: 'Combine elements from two sets',
      example: 'all_items := set1 ∪ set2'
    },
    {
      symbol: '∩',
      name: 'Intersection',
      unicode: 'U+2229',
      category: 'Set Operation',
      value: 'Set intersection operator',
      usage: 'Find common elements between sets',
      example: 'common := set1 ∩ set2'
    },
    {
      symbol: '∈',
      name: 'Element Of',
      unicode: 'U+2208',
      category: 'Set Operation',
      value: 'Membership operator',
      usage: 'Check if element is in set',
      example: 'if x ∈ validValues { process(x) }'
    },
    {
      symbol: '∉',
      name: 'Not Element Of',
      unicode: 'U+2209',
      category: 'Set Operation',
      value: 'Non-membership operator',
      usage: 'Check if element is not in set',
      example: 'if y ∉ blacklist { allow(y) }'
    },
    
    // Statistical Symbols
    {
      symbol: 'μ',
      name: 'Mu (Mean)',
      unicode: 'U+03BC',
      category: 'Statistical Function',
      value: 'Population mean function',
      usage: 'Calculate average of dataset',
      example: 'average := μ(dataset)'
    },
    {
      symbol: 'σ',
      name: 'Sigma (Standard Deviation)',
      unicode: 'U+03C3',
      category: 'Statistical Function',
      value: 'Population standard deviation',
      usage: 'Measure of data spread',
      example: 'spread := σ(values)'
    },
    {
      symbol: 'σ²',
      name: 'Sigma Squared (Variance)',
      unicode: 'U+03C3 U+00B2',
      category: 'Statistical Function',
      value: 'Population variance',
      usage: 'Square of standard deviation',
      example: 'variance := σ²(data)'
    },
    
    // Mathematical Operators
    {
      symbol: 'Σ',
      name: 'Capital Sigma (Summation)',
      unicode: 'U+03A3',
      category: 'Mathematical Operator',
      value: 'Summation operator',
      usage: 'Sum over a range',
      example: 'total := Σ(i in 1..n) { i² }'
    },
    {
      symbol: 'Π',
      name: 'Capital Pi (Product)',
      unicode: 'U+03A0',
      category: 'Mathematical Operator',
      value: 'Product operator',
      usage: 'Product over a range',
      example: 'factorial := Π(i in 1..n) { i }'
    },
    
    // Other Greek Letters
    {
      symbol: 'α',
      name: 'Alpha',
      unicode: 'U+03B1',
      category: 'Greek Letter',
      value: 'First Greek letter',
      usage: 'Angles, coefficients, parameters',
      example: 'angle_α := 45°'
    },
    {
      symbol: 'β',
      name: 'Beta',
      unicode: 'U+03B2',
      category: 'Greek Letter',
      value: 'Second Greek letter',
      usage: 'Angles, coefficients, beta testing',
      example: 'β_coefficient := 0.5'
    },
    {
      symbol: 'δ',
      name: 'Delta (lowercase)',
      unicode: 'U+03B4',
      category: 'Greek Letter',
      value: 'Small change or difference',
      usage: 'Small variations, Dirac delta',
      example: 'δx := 0.001'
    },
    {
      symbol: 'Δ',
      name: 'Delta (uppercase)',
      unicode: 'U+0394',
      category: 'Greek Letter',
      value: 'Change or difference',
      usage: 'Change in value, discriminant',
      example: 'Δt := t2 - t1'
    },
    {
      symbol: 'λ',
      name: 'Lambda',
      unicode: 'U+03BB',
      category: 'Greek Letter',
      value: 'Lambda functions',
      usage: 'Anonymous functions, wavelength',
      example: 'square := λ x => x²'
    },
    {
      symbol: 'ρ',
      name: 'Rho',
      unicode: 'U+03C1',
      category: 'Greek Letter',
      value: 'Density, correlation',
      usage: 'Density calculations, correlation coefficient',
      example: 'density_ρ := mass / volume'
    },
    
    // Special Operators
    {
      symbol: ':=',
      name: 'Walrus Operator',
      unicode: ':=',
      category: 'Assignment Operator',
      value: 'Type-inferred assignment',
      usage: 'Declare and assign with type inference',
      example: 'result := calculateValue()'
    },
    {
      symbol: '∀',
      name: 'For All',
      unicode: 'U+2200',
      category: 'Logical Quantifier',
      value: 'Universal quantifier',
      usage: 'Express "for all" in logic',
      example: '∀ x in set: property(x)'
    },
    {
      symbol: '∃',
      name: 'There Exists',
      unicode: 'U+2203',
      category: 'Logical Quantifier',
      value: 'Existential quantifier',
      usage: 'Express "there exists" in logic',
      example: '∃ x in set: condition(x)'
    },
    {
      symbol: '≈',
      name: 'Approximately Equal',
      unicode: 'U+2248',
      category: 'Comparison Operator',
      value: 'Approximate equality',
      usage: 'Compare floating-point values',
      example: 'if result ≈ expected { success() }'
    },
    {
      symbol: '≠',
      name: 'Not Equal',
      unicode: 'U+2260',
      category: 'Comparison Operator',
      value: 'Inequality operator',
      usage: 'Check if values are different',
      example: 'if a ≠ b { handleDifference() }'
    },
    {
      symbol: '≤',
      name: 'Less Than or Equal',
      unicode: 'U+2264',
      category: 'Comparison Operator',
      value: 'Less than or equal comparison',
      usage: 'Compare values',
      example: 'if score ≤ threshold { retry() }'
    },
    {
      symbol: '≥',
      name: 'Greater Than or Equal',
      unicode: 'U+2265',
      category: 'Comparison Operator',
      value: 'Greater than or equal comparison',
      usage: 'Compare values',
      example: 'if age ≥ 18 { allowAccess() }'
    }
  ];

  const groupedSymbols = symbols.reduce((acc, symbol) => {
    if (!acc[symbol.category]) {
      acc[symbol.category] = [];
    }
    acc[symbol.category].push(symbol);
    return acc;
  }, {});

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-4xl font-bold mb-4">Symbol Glossary</h1>
        <p className="text-lg text-gray-600 dark:text-gray-400">
          Complete reference of all unique symbols, operators, and constants in Ouroboros.
        </p>
      </div>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Quick Reference</h2>
        <div className="bg-gray-50 dark:bg-gray-800 p-6 rounded-lg">
          <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
            {symbols.map((symbol, index) => (
              <div key={index} className="text-center">
                <div className="text-2xl font-mono mb-1">{symbol.symbol}</div>
                <div className="text-xs text-gray-600 dark:text-gray-400">{symbol.name}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {Object.entries(groupedSymbols).map(([category, categorySymbols]) => (
        <section key={category} className="space-y-6">
          <h2 className="text-3xl font-semibold mb-4">{category}</h2>
          <div className="space-y-6">
            {categorySymbols.map((symbol, index) => (
              <div key={index} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex items-center space-x-4">
                    <span className="text-4xl font-mono">{symbol.symbol}</span>
                    <div>
                      <h3 className="text-xl font-semibold">{symbol.name}</h3>
                      <p className="text-sm text-gray-600 dark:text-gray-400">Unicode: {symbol.unicode}</p>
                    </div>
                  </div>
                  <span className="text-sm px-3 py-1 bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 rounded-full">
                    {symbol.category}
                  </span>
                </div>
                
                <div className="space-y-2 mb-4">
                  <p><strong>Value:</strong> {symbol.value}</p>
                  <p><strong>Usage:</strong> {symbol.usage}</p>
                </div>
                
                <div className="mt-4">
                  <p className="text-sm font-semibold mb-2">Example:</p>
                  <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded font-mono text-sm">
                    {symbol.example}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </section>
      ))}

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Symbol Input Methods</h2>
        <div className="bg-gray-50 dark:bg-gray-800 p-6 rounded-lg">
          <h3 className="text-xl font-semibold mb-4">How to Type These Symbols</h3>
          <div className="space-y-4">
            <div>
              <h4 className="font-semibold mb-2">Windows</h4>
              <ul className="list-disc list-inside space-y-1 text-sm">
                <li>Use Windows + . (period) to open emoji/symbol picker</li>
                <li>Use Alt codes (hold Alt + numeric keypad)</li>
                <li>Copy from Character Map application</li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-2">macOS</h4>
              <ul className="list-disc list-inside space-y-1 text-sm">
                <li>Use Ctrl + Cmd + Space for character viewer</li>
                <li>Use Option key combinations</li>
                <li>Enable Greek keyboard in System Preferences</li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-2">Linux</h4>
              <ul className="list-disc list-inside space-y-1 text-sm">
                <li>Use Ctrl + Shift + U followed by Unicode code</li>
                <li>Use compose key sequences</li>
                <li>Install and use Character Map application</li>
              </ul>
            </div>
            <div>
              <h4 className="font-semibold mb-2">IDE Support</h4>
              <ul className="list-disc list-inside space-y-1 text-sm">
                <li>Most modern IDEs support Unicode input</li>
                <li>Use IDE-specific symbol palettes</li>
                <li>Configure custom abbreviations/snippets</li>
                <li>Copy from this glossary page</li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section className="space-y-6">
        <h2 className="text-3xl font-semibold mb-4">Usage Guidelines</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="bg-blue-50 dark:bg-blue-900/20 p-6 rounded-lg">
            <h3 className="text-lg font-semibold mb-3">Best Practices</h3>
            <ul className="space-y-2 text-sm">
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>Use symbols for mathematical operations where they improve clarity</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>Prefer Greek letters for mathematical constants and physics</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>Use set operators for collection operations</span>
              </li>
              <li className="flex items-start">
                <span className="text-green-500 mr-2">✓</span>
                <span>Consider readability for team members</span>
              </li>
            </ul>
          </div>
          
          <div className="bg-red-50 dark:bg-red-900/20 p-6 rounded-lg">
            <h3 className="text-lg font-semibold mb-3">Things to Avoid</h3>
            <ul className="space-y-2 text-sm">
              <li className="flex items-start">
                <span className="text-red-500 mr-2">✗</span>
                <span>Don't overuse symbols where words are clearer</span>
              </li>
              <li className="flex items-start">
                <span className="text-red-500 mr-2">✗</span>
                <span>Avoid symbols that might not render on all systems</span>
              </li>
              <li className="flex items-start">
                <span className="text-red-500 mr-2">✗</span>
                <span>Don't mix similar-looking symbols (ρ vs p)</span>
              </li>
              <li className="flex items-start">
                <span className="text-red-500 mr-2">✗</span>
                <span>Avoid creating custom meanings for standard symbols</span>
              </li>
            </ul>
          </div>
        </div>
      </section>
    </div>
  );
};

export default GlossaryPage; 