import { AlertCircle, Info, Lightbulb, AlertTriangle, CheckCircle } from 'lucide-react'

const variants = {
  info: {
    icon: Info,
    className: 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800 text-blue-800 dark:text-blue-200',
    iconClassName: 'text-blue-600 dark:text-blue-400'
  },
  tip: {
    icon: Lightbulb,
    className: 'bg-green-50 dark:bg-green-900/20 border-green-200 dark:border-green-800 text-green-800 dark:text-green-200',
    iconClassName: 'text-green-600 dark:text-green-400'
  },
  warning: {
    icon: AlertTriangle,
    className: 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-800 text-yellow-800 dark:text-yellow-200',
    iconClassName: 'text-yellow-600 dark:text-yellow-400'
  },
  danger: {
    icon: AlertCircle,
    className: 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800 text-red-800 dark:text-red-200',
    iconClassName: 'text-red-600 dark:text-red-400'
  },
  success: {
    icon: CheckCircle,
    className: 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-200',
    iconClassName: 'text-emerald-600 dark:text-emerald-400'
  }
}

export default function Callout({ type = 'info', title, children }) {
  const variant = variants[type] || variants.info
  const Icon = variant.icon

  return (
    <div className={`rounded-lg border p-4 my-6 ${variant.className}`}>
      <div className="flex">
        <Icon className={`h-5 w-5 mr-3 flex-shrink-0 mt-0.5 ${variant.iconClassName}`} />
        <div className="flex-1">
          {title && (
            <h4 className="font-semibold mb-1">{title}</h4>
          )}
          <div className="prose prose-sm max-w-none dark:prose-invert">
            {children}
          </div>
        </div>
      </div>
    </div>
  )
} 