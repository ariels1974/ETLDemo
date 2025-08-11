import { useEffect, useState } from "react";
import "./App.css";

function App() {
  const [records, setRecords] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchRecords = () => {
    setLoading(true);
    setError(null);
    fetch("/api/products")
      .then((res) => {
        if (!res.ok) throw new Error("Failed to fetch");
        return res.json();
      })
      .then(setRecords)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  };

  return (
    <div className="App">
      <h1>Product Scraping Records</h1>
      <button
        onClick={fetchRecords}
        disabled={loading}
        style={{ marginBottom: 16 }}
      >
        {loading ? "Loading..." : "Fetch Products"}
      </button>
      {error && <p style={{ color: "red" }}>Error: {error}</p>}
      {records.length > 0 && !loading && !error && (
        <table
          border="1"
          cellPadding="8"
          style={{ margin: "auto", minWidth: 400 }}
        >
          <thead>
            <tr>
              {records[0] &&
                Object.keys(records[0]).map((key) => <th key={key}>{key}</th>)}
            </tr>
          </thead>
          <tbody>
            {records.map((rec, idx) => (
              <tr key={rec._id || idx}>
                {Object.values(rec).map((val, i) => (
                  <td key={i}>
                    {typeof val === "object"
                      ? JSON.stringify(val)
                      : String(val)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default App;
