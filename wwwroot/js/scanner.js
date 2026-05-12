(() => {
  let currentScanKey = null;

  const getEl = (id) => document.getElementById(id);

  async function loadScanners() {
    const status = getEl("scanStatus");
    const select = getEl("scannerSelect");
    const scanBtn = getEl("scanBtn");

    if (!status || !select || !scanBtn) return;

    status.textContent = "Duke kërkuar skanerët...";

    try {
      const response = await fetch("http://localhost:7331/scanners");
      const scanners = await response.json();

      select.innerHTML = "";
      scanners.forEach((s) => {
        const opt = document.createElement("option");
        opt.value = s;
        opt.textContent = s;
        select.appendChild(opt);
      });

      select.disabled = false;
      scanBtn.disabled = false;
      status.textContent = scanners.length + " skaner gjetur.";
    } catch {
      status.textContent = "Shërbimi i skanerit nuk është aktiv.";
    }
  }

  async function startScan() {
    const scannerSelect = getEl("scannerSelect");
    const status = getEl("scanStatus");
    const scanBtn = getEl("scanBtn");

    if (!scannerSelect || !status || !scanBtn) return;

    const scanner = scannerSelect.value;
    if (!scanner) return;

    status.textContent = "Duke skanuar...";
    scanBtn.disabled = true;

    const subfolder = window.scanSubfolder || "outgoing";

    try {
      const res = await fetch(
        `http://localhost:7331/scan?subfolder=${encodeURIComponent(subfolder)}&scanner=${encodeURIComponent(scanner)}`,
        { method: "POST" }
      );

      const result = await res.json();

      if (result.success) {
        currentScanKey = result.sessionKey;
        const hidden = getEl("scanSessionKey");
        if (hidden) hidden.value = result.sessionKey;
        status.textContent = "Skanimi u krye me sukses!";
      }
    } catch {
      status.textContent = "Gabim gjatë skanimit.";
    } finally {
      scanBtn.disabled = false;
    }
  }

  function cancelScan() {
    if (currentScanKey) {
      navigator.sendBeacon("/api/scan/cancel?key=" + currentScanKey);
      currentScanKey = null;
    }
  }

  function initScanner(subfolder) {
    if (subfolder) window.scanSubfolder = subfolder;
    window.addEventListener("beforeunload", cancelScan);
    loadScanners();
  }

  window.loadScanners = loadScanners;
  window.startScan = startScan;
  window.cancelScan = cancelScan;
  window.initScanner = initScanner;
})();