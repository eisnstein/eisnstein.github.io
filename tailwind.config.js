/** @type {import('tailwindcss').Config} */
const defaultTheme = require('tailwindcss/defaultTheme')

module.exports = {
  content: [
    './_drafts/**/*.html',
    './_includes/**/*.html',
    './_layouts/**/*.html',
    './_posts/*.md',
    './*.md',
    './*.html',
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: '#072344',
      },
      fontFamily: {
        sans: ['Inter', ...defaultTheme.fontFamily.sans],
      },
    },
    fontWeight: {
      light: 300,
      normal: 400,
      medium: 500,
      bold: 700,
    },
  },
  plugins: [require('@tailwindcss/typography')],
}
