/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./wwwroot/index.html",
    "./wwwroot/**/*.html",
    "./Components/**/*.{razor,cshtml,html}",
    "./**/*.razor"
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: "#f7f9ff",
          100: "#e9f0ff",
          300: "#c7d7ff",
          500: "#7a8cf5",
          700: "#4c5cc4"
        }
      },
      fontFamily: {
        display: ["Plus Jakarta Sans", "Segoe UI", "sans-serif"],
        body: ["Plus Jakarta Sans", "Segoe UI", "sans-serif"]
      }
    }
  },
  plugins: []
};
