import { useState } from "react";
import "./App.css";

function App() {
  const [records, setRecords] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState("");
  const [sortKey, setSortKey] = useState("");
  const [sortAsc, setSortAsc] = useState(true);

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

  // Filter and sort logic
  const filteredRecords = records
    .filter((rec) =>
      filter
        ? Object.values(rec)
            .join(" ")
            .toLowerCase()
            .includes(filter.toLowerCase())
        : true
    )
    .sort((a, b) => {
      if (!sortKey) return 0;
      const aVal = a[sortKey];
      const bVal = b[sortKey];
      if (aVal === undefined || bVal === undefined) return 0;
      if (typeof aVal === "string" && typeof bVal === "string") {
        if (sortAsc) return aVal.localeCompare(bVal);
        return bVal.localeCompare(aVal);
      }
      if (aVal === bVal) return 0;
      if (sortAsc) return aVal > bVal ? 1 : -1;
      return aVal < bVal ? 1 : -1;
    });

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
      {filteredRecords.length > 0 && !loading && !error && (
        <div
          style={{ display: "flex", alignItems: "center", marginBottom: 16 }}
        >
          <input
            type="text"
            placeholder="Filter products..."
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            style={{ padding: 4, minWidth: 200, marginRight: 16 }}
          />
          <label style={{ marginRight: 8 }}>Sort by:</label>
          <select
            value={sortKey}
            onChange={(e) => setSortKey(e.target.value)}
            style={{ padding: 4, minWidth: 120, marginRight: 8 }}
          >
            <option value="">-- Select --</option>
            {filteredRecords[0] &&
              Object.keys(filteredRecords[0]).map((key) => (
                <option key={key} value={key}>
                  {key}
                </option>
              ))}
          </select>
          <button
            onClick={() => setSortAsc((a) => !a)}
            style={{ padding: 4 }}
            disabled={!sortKey}
            title="Toggle sort direction"
          >
            {sortAsc ? "▲" : "▼"}
          </button>
        </div>
      )}
      {filteredRecords.length > 0 && !loading && !error && (
        <table
          border="1"
          cellPadding="8"
          style={{ margin: "auto", minWidth: 400 }}
        >
          <thead>
            <tr>
              {filteredRecords[0] &&
                Object.keys(filteredRecords[0]).map((key) => (
                  <th key={key}>{key}</th>
                ))}
            </tr>
          </thead>
          <tbody>
            {filteredRecords.map((rec, idx) => (
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
