// Import Firebase SDKs
import { initializeApp } from "https://www.gstatic.com/firebasejs/9.22.0/firebase-app.js";
import { getDatabase, ref, get, set, child, update, remove } from "https://www.gstatic.com/firebasejs/9.22.0/firebase-database.js";

// --- CONFIGURATION: PASTE YOUR KEYS HERE ---
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
  apiKey: "AIzaSyBh8rTtRssy9j7UELcVmbilv7cQwFtDl8o",
  authDomain: "galaxybook-1bbc7.firebaseapp.com",
  databaseURL: "https://galaxybook-1bbc7-default-rtdb.firebaseio.com",
  projectId: "galaxybook-1bbc7",
  storageBucket: "galaxybook-1bbc7.firebasestorage.app",
  messagingSenderId: "424415030072",
  appId: "1:424415030072:web:c2790e3d465e3cd3992fcd",
  measurementId: "G-SX2KFZ3PXW"
};

const app = initializeApp(firebaseConfig);
const db = getDatabase(app);

// GLOBAL STATE
let allEmployees = [];
let allRates = [];
let loadedRowIds = []; // Stores IDs of rows currently in Data Entry grid

// --- INITIALIZATION ---
window.onload = () => {
    // Set Today's Date
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('entryDate').value = today;
    document.getElementById('reportFrom').value = today;
    document.getElementById('reportTo').value = today;

    // Load Data
    loadEmployeeMaster();
    loadRates();
    
    // Create initial empty rows for Data Entry
    for(let i=0; i<10; i++) addEntryRow();

    // Create Rate Inputs (A-G)
    const rateContainer = document.getElementById('rateInputs');
    ['A','B','C','D','E','F','G'].forEach(lbl => {
        rateContainer.innerHTML += `
            <div class="col-6">
                <div class="input-group">
                    <span class="input-group-text fw-bold" style="width:40px">${lbl}</span>
                    <input type="number" id="rateVal_${lbl}" class="form-control" value="0">
                </div>
            </div>`;
    });
};

// --- NAVIGATION ---
window.showTab = (id) => {
    document.querySelectorAll('.tab-section').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.nav-link').forEach(el => el.classList.remove('active'));
    document.getElementById(id).style.display = 'block';
    event.target.classList.add('active');
};

// ============================================
// 1. EMPLOYEE MASTER LOGIC
// ============================================
async function loadEmployeeMaster() {
    const snapshot = await get(child(ref(db), 'EmployeeMaster'));
    if(snapshot.exists()) {
        const data = snapshot.val();
        allEmployees = Array.isArray(data) ? data : Object.values(data);
    } else {
        allEmployees = [];
    }
    renderMasterTable();
    loadEmployeeDropdown();
}

function renderMasterTable() {
    const tbody = document.querySelector('#masterTable tbody');
    tbody.innerHTML = '';
    const filter = document.getElementById('masterSearch').value.toLowerCase();

    allEmployees.forEach(emp => {
        if(emp && (emp.EnglishName.toLowerCase().includes(filter) || emp.GujaratiName.includes(filter))) {
            const tr = document.createElement('tr');
            tr.innerHTML = `<td>${emp.EmployeeID}</td><td>${emp.EnglishName}</td><td>${emp.GujaratiName}</td><td>${emp.EntryType}</td><td>${emp.Active}</td>`;
            tr.style.cursor = 'pointer';
            tr.onclick = () => fillMasterForm(emp);
            tbody.appendChild(tr);
        }
    });
}

window.filterMasterTable = renderMasterTable;

window.fillMasterForm = (emp) => {
    document.getElementById('mstId').value = emp.EmployeeID;
    document.getElementById('mstEnglish').value = emp.EnglishName;
    document.getElementById('mstGujarati').value = emp.GujaratiName;
    document.getElementById('mstType').value = emp.EntryType;
    document.getElementById('mstActive').value = emp.Active;
};

window.clearMasterForm = () => {
    document.getElementById('mstId').value = '';
    document.getElementById('mstEnglish').value = '';
    document.getElementById('mstGujarati').value = '';
    document.getElementById('mstEnglish').focus();
};

window.saveMaster = async () => {
    const idVal = document.getElementById('mstId').value;
    const eng = document.getElementById('mstEnglish').value;
    const guj = document.getElementById('mstGujarati').value || eng; // Fallback
    const type = document.getElementById('mstType').value;
    const active = document.getElementById('mstActive').value;

    if(!eng) return alert("Name is required");

    let newId = idVal ? parseInt(idVal) : (allEmployees.length > 0 ? Math.max(...allEmployees.map(e => e.EmployeeID || 0)) + 1 : 1);
    
    const newEmp = { EmployeeID: newId, EnglishName: eng, GujaratiName: guj, EntryType: type, Active: active };

    // Update existing or add new
    const idx = allEmployees.findIndex(e => e.EmployeeID == newId);
    if(idx >= 0) allEmployees[idx] = newEmp;
    else allEmployees.push(newEmp);

    await set(ref(db, 'EmployeeMaster'), allEmployees);
    alert('Saved Successfully!');
    loadEmployeeMaster();
    clearMasterForm();
};

// ============================================
// 2. DATA ENTRY LOGIC
// ============================================
window.loadEmployeeDropdown = () => {
    const type = document.getElementById('entryType').value;
    const datalist = document.getElementById('empOptions');
    datalist.innerHTML = '';
    allEmployees.filter(e => e.EntryType === type && e.Active === 'Yes').forEach(e => {
        const opt = document.createElement('option');
        opt.value = e.EnglishName;
        datalist.appendChild(opt);
    });
};

document.getElementById('entryEmpName').addEventListener('change', function() {
    const emp = allEmployees.find(e => e.EnglishName === this.value);
    if(emp) document.getElementById('entryGujName').value = emp.GujaratiName;
});

window.addEntryRow = (data = {}) => {
    const tbody = document.querySelector('#entryTable tbody');
    const tr = document.createElement('tr');
    tr.dataset.id = data.Id || ''; 
    
    ['A','B','C','D','E','F','G','Ct'].forEach(col => {
        tr.innerHTML += `<td><input type="text" name="Col_${col}" value="${data['Col_'+col] || ''}"></td>`;
    });
    tr.innerHTML += `<td class="text-center"><button class="btn btn-sm btn-danger" onclick="this.closest('tr').remove()"><i class="fa fa-times"></i></button></td>`;
    tbody.appendChild(tr);
};

window.loadEntryData = async () => {
    const name = document.getElementById('entryEmpName').value;
    const date = document.getElementById('entryDate').value;
    const type = document.getElementById('entryType').value;

    if(!name) return alert("Select Name first");

    // Fetch all data (In real app, consider filtering via query)
    const snap = await get(child(ref(db), 'EmployeeData'));
    if(!snap.exists()) return alert("No Data Found");

    let allData = Object.values(snap.val());
    // Filter locally
    const filtered = allData.filter(x => x.EmpName_English === name && x.EntryDate.startsWith(date) && x.EntryType === type);

    const tbody = document.querySelector('#entryTable tbody');
    tbody.innerHTML = '';
    
    if(filtered.length > 0) {
        document.getElementById('entryUppad').value = filtered[0].Uppad || 0;
        filtered.forEach(row => addEntryRow(row));
        loadedRowIds = filtered.map(x => x.Id);
        document.getElementById('btnSave').style.display = 'none';
        document.getElementById('btnUpdate').style.display = 'inline-block';
        alert("Data Loaded");
    } else {
        document.getElementById('entryUppad').value = 0;
        for(let i=0; i<10; i++) addEntryRow();
        document.getElementById('btnSave').style.display = 'inline-block';
        document.getElementById('btnUpdate').style.display = 'none';
        alert("No existing data found.");
    }
};

window.saveEntryData = async (isUpdate) => {
    const name = document.getElementById('entryEmpName').value;
    const guj = document.getElementById('entryGujName').value;
    const date = document.getElementById('entryDate').value;
    const type = document.getElementById('entryType').value;
    const uppad = document.getElementById('entryUppad').value;

    if(!name) return alert("Name Required");

    const snap = await get(child(ref(db), 'EmployeeData'));
    let allData = snap.exists() ? (Array.isArray(snap.val()) ? snap.val() : Object.values(snap.val())) : [];

    // Get Next ID
    let nextId = allData.length > 0 ? Math.max(...allData.map(x => x.Id || 0)) + 1 : 1;

    // Remove old data if updating
    if(isUpdate) {
        allData = allData.filter(x => !(x.EmpName_English === name && x.EntryDate.startsWith(date) && x.EntryType === type));
    }

    // Collect Grid Data
    const rows = document.querySelectorAll('#entryTable tbody tr');
    let hasData = false;

    rows.forEach(tr => {
        const inputs = tr.querySelectorAll('input');
        // Check if row has any value
        if([...inputs].some(i => i.value.trim() !== "")) {
            hasData = true;
            let rowId = (isUpdate && tr.dataset.id) ? parseInt(tr.dataset.id) : nextId++;
            
            allData.push({
                Id: rowId,
                EmpName_English: name,
                EmpName_Gujarati: guj,
                EntryDate: date,
                EntryType: type,
                Uppad: uppad,
                Col_A: inputs[0].value, Col_B: inputs[1].value, Col_C: inputs[2].value, Col_D: inputs[3].value,
                Col_E: inputs[4].value, Col_F: inputs[5].value, Col_G: inputs[6].value, Col_Ct: inputs[7].value
            });
        }
    });

    if(hasData) {
        await set(ref(db, 'EmployeeData'), allData);
        alert("Saved Successfully!");
        // Reset
        document.querySelector('#entryTable tbody').innerHTML = '';
        for(let i=0; i<10; i++) addEntryRow();
        document.getElementById('btnUpdate').style.display = 'none';
        document.getElementById('btnSave').style.display = 'inline-block';
    }
};

// ============================================
// 3. RATE MASTER LOGIC
// ============================================
window.loadRates = async () => {
    const snap = await get(child(ref(db), 'RateMaster'));
    if(snap.exists()) {
        allRates = Array.isArray(snap.val()) ? snap.val() : Object.values(snap.val());
        const type = document.getElementById('rateType').value;
        const r = allRates.find(x => x.RateType === type);
        
        ['A','B','C','D','E','F','G'].forEach(lbl => {
            document.getElementById(`rateVal_${lbl}`).value = r ? r[`Val_${lbl}`] : 0;
        });
    }
};

window.saveRates = async () => {
    const type = document.getElementById('rateType').value;
    let existing = allRates.find(x => x.RateType === type);
    if(!existing) {
        existing = { RateType: type };
        allRates.push(existing);
    }
    
    ['A','B','C','D','E','F','G'].forEach(lbl => {
        existing[`Val_${lbl}`] = parseFloat(document.getElementById(`rateVal_${lbl}`).value) || 0;
    });

    await set(ref(db, 'RateMaster'), allRates);
    alert('Rates Saved!');
};

// ============================================
// 4. REPORT LOGIC
// ============================================
window.generateReport = async () => {
    const from = document.getElementById('reportFrom').value;
    const to = document.getElementById('reportTo').value;
    const type = document.getElementById('reportType').value;

    // Get fresh data
    const snapData = await get(child(ref(db), 'EmployeeData'));
    const allEntries = snapData.exists() ? Object.values(snapData.val()) : [];
    
    await loadRates();
    const r = allRates.find(x => x.RateType === type) || {};

    // Build Headers
    const thead = document.querySelector('#reportTable thead');
    thead.innerHTML = `
        <tr>
            <th>Name</th>
            <th>${r.Val_A || 'A'}</th> <th>${r.Val_B || 'B'}</th> <th>${r.Val_C || 'C'}</th>
            <th>${r.Val_D || 'D'}</th> <th>${r.Val_E || 'E'}</th> <th>${r.Val_F || 'F'}</th> <th>${r.Val_G || 'G'}</th>
            <th>Ct</th> <th>Total</th> <th>Amount</th> <th>Uppad</th> <th>Jama</th>
        </tr>`;

    // Filter
    const filtered = allEntries.filter(x => {
        const d = x.EntryDate.split('T')[0];
        return d >= from && d <= to && x.EntryType === type;
    });

    // Grouping
    const groups = {};
    filtered.forEach(x => {
        if(!groups[x.EmpName_English]) {
            groups[x.EmpName_English] = { name: x.EmpName_English, A:0,B:0,C:0,D:0,E:0,F:0,G:0,Ct:0, entries: [] };
        }
        const g = groups[x.EmpName_English];
        g.A += parseInt(x.Col_A)||0; g.B += parseInt(x.Col_B)||0; g.C += parseInt(x.Col_C)||0;
        g.D += parseInt(x.Col_D)||0; g.E += parseInt(x.Col_E)||0; g.F += parseInt(x.Col_F)||0;
        g.G += parseInt(x.Col_G)||0; g.Ct += parseFloat(x.Col_Ct)||0;
        g.entries.push(x);
    });

    // Render Rows
    const tbody = document.querySelector('#reportTable tbody');
    tbody.innerHTML = '';
    
    let gKam = 0, gUppad = 0, gJama = 0;

    Object.values(groups).forEach(g => {
        // Calculate Uppad (Max per day)
        const dayMap = {};
        g.entries.forEach(e => {
            const d = e.EntryDate.split('T')[0];
            dayMap[d] = Math.max(dayMap[d] || 0, parseInt(e.Uppad)||0);
        });
        const totalUppad = Object.values(dayMap).reduce((a,b)=>a+b,0);

        const totalKam = (g.A*(r.Val_A||0)) + (g.B*(r.Val_B||0)) + (g.C*(r.Val_C||0)) + 
                         (g.D*(r.Val_D||0)) + (g.E*(r.Val_E||0)) + (g.F*(r.Val_F||0)) + (g.Ct*(r.Val_G||0));

        const totalCnt = g.A+g.B+g.C+g.D+g.E+g.F+g.G;
        const jama = totalKam - totalUppad;

        gKam += totalKam; gUppad += totalUppad; gJama += jama;

        tbody.innerHTML += `
            <tr>
                <td>${g.name}</td>
                <td>${g.A||''}</td> <td>${g.B||''}</td> <td>${g.C||''}</td> <td>${g.D||''}</td>
                <td>${g.E||''}</td> <td>${g.F||''}</td> <td>${g.G||''}</td> <td>${g.Ct||''}</td>
                <td>${totalCnt}</td>
                <td class="fw-bold">${totalKam.toLocaleString()}</td>
                <td class="text-danger">${totalUppad.toLocaleString()}</td>
                <td class="text-success fw-bold">${jama.toLocaleString()}</td>
            </tr>`;
    });

    document.getElementById('reportTotal').innerText = `Total Work: ${gKam.toLocaleString()} | Total Uppad: ${gUppad.toLocaleString()} | Net Jama: ${gJama.toLocaleString()}`;
};

// ============================================
// 5. BACKUP LOGIC
// ============================================
window.downloadBackup = async () => {
    const snapM = await get(child(ref(db), 'EmployeeMaster'));
    const snapD = await get(child(ref(db), 'EmployeeData'));
    const snapR = await get(child(ref(db), 'RateMaster'));

    const backup = {
        EmployeeMaster: snapM.val(),
        EmployeeData: snapD.val(),
        RateMaster: snapR.val()
    };
    
    const blob = new Blob([JSON.stringify(backup, null, 2)], {type : 'application/json'});
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `GalaxyBackup_${new Date().toISOString().split('T')[0]}.json`;
    a.click();
};

window.restoreBackup = () => {
    const file = document.getElementById('backupFile').files[0];
    if(!file) return;
    const reader = new FileReader();
    reader.onload = async (e) => {
        const json = JSON.parse(e.target.result);
        if(confirm("Are you sure? This will overwrite everything.")) {
            if(json.EmployeeMaster) await set(ref(db, 'EmployeeMaster'), json.EmployeeMaster);
            if(json.EmployeeData) await set(ref(db, 'EmployeeData'), json.EmployeeData);
            if(json.RateMaster) await set(ref(db, 'RateMaster'), json.RateMaster);
            alert("Restored Successfully");
            location.reload();
        }
    };
    reader.readAsText(file);
};