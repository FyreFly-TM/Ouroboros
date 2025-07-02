import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import Layout from './components/Layout'
import HomePage from './pages/HomePage'
import MathSymbolsPage from './pages/MathSymbolsPage'
import HighLevelSyntaxPage from './pages/HighLevelSyntaxPage'
import MediumLevelSyntaxPage from './pages/MediumLevelSyntaxPage'
import LowLevelSyntaxPage from './pages/LowLevelSyntaxPage'
import UIFrameworkPage from './pages/UIFrameworkPage'
import CollectionsPage from './pages/CollectionsPage'
import LinearAlgebraPage from './pages/LinearAlgebraPage'
import FileIOPage from './pages/FileIOPage'
import DateTimePage from './pages/DateTimePage'
import GlossaryPage from './pages/GlossaryPage'

function App() {
  return (
    <Router>
      <Layout>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/math-symbols" element={<MathSymbolsPage />} />
          <Route path="/high-level-syntax" element={<HighLevelSyntaxPage />} />
          <Route path="/medium-level-syntax" element={<MediumLevelSyntaxPage />} />
          <Route path="/low-level-syntax" element={<LowLevelSyntaxPage />} />
          <Route path="/ui-framework" element={<UIFrameworkPage />} />
          <Route path="/collections" element={<CollectionsPage />} />
          <Route path="/linear-algebra" element={<LinearAlgebraPage />} />
          <Route path="/file-io" element={<FileIOPage />} />
          <Route path="/datetime" element={<DateTimePage />} />
          <Route path="/glossary" element={<GlossaryPage />} />
        </Routes>
      </Layout>
    </Router>
  )
}

export default App 