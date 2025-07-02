Build a complete local documentation site for my custom programming language, Ouroboros. It should run at localhost:PORT and include thorough documentation of every feature demonstrated in the provided Ultimate Ouroboros Language Test Script (see code below).

Documentation Content
For each test section in the script (MathSymbols, HighLevelSyntax, MediumLevelSyntax, LowLevelSyntax, UIFramework, Collections, LinearAlgebra, FileIO, DateTime):

Provide a detailed explanation of what each function, keyword, syntax, and unique feature does.

Include usage examples pulled directly from the script, but reformat them nicely for the web (with syntax highlighting).

Add clear instructions on how to use each feature in actual Ouroboros programs.

For things using Greek symbols (like π, φ, ∞), explain their purpose, value, and how they’re used.

For high-level syntax (repeat, match, if version is greater than, etc.), explain it like teaching a beginner.

For medium-level syntax (C-like constructs), describe how they differ from traditional languages.

For low-level syntax (unsafe, manual memory, bit manipulation, SIMD, etc.), explain risks, performance considerations, and when/why to use them.

For the UI framework test, document all the UI elements (MenuBar, ToolBar, Window, TabControl, Button, Label, etc.) with examples of how to create and configure them.

For Collections, Linear Algebra, File I/O, DateTime, explain each API used, their methods, and showcase practical code snippets.

Include a section for each standard library module used in the script (IO, Math, UI, System, Collections), describing what it provides.

Provide full working examples showing how to set up a small Ouroboros program using features from each section.

Add a glossary page listing all unique symbols (like ∪, ∩, ∈, ∉, Σ, Π) with their meaning, Unicode, and usage in Ouroboros.

UI Requirements
Build the site using React, Vite, and Tailwind CSS for a sleek, modern, responsive design.

Create a dark mode/light mode toggle.

Add a search bar to quickly find features or keywords in the docs.

Use a sidebar navigation menu organized by main topics:

diff
Copy
Edit
- Introduction
- MathSymbols
- HighLevelSyntax
- MediumLevelSyntax
- LowLevelSyntax
- UIFramework
- Collections
- LinearAlgebra
- FileIO
- DateTime
- Glossary
At the top of each page, include a breadcrumb navigation like:

nginx
Copy
Edit
Home / Collections / HashSet
Each page should have a beautiful header with the Ouroboros logo (placeholder is fine) and page title.

Add syntax-highlighted code blocks for examples.

Include callouts (like “💡 Tip”, “⚠️ Warning”) for important notes.

Ensure typography and layout are professional and readable on both desktop and mobile.

Project Setup
The output should be a complete, working Vite React project in a folder structure like:

arduino
Copy
Edit
my-ourobobos-docs/
  src/
    components/
    pages/
    assets/
    App.jsx
    main.jsx
  public/
  index.html
  tailwind.config.js
  vite.config.js
  package.json
All content must be local (no external API calls).

The site must be self-contained, ready to run with npm install && npm run dev.

Provided Script
Use the message Example.txt as the sole source of features and examples

End of prompt.