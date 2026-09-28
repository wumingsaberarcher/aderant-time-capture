const state = {
  lookups: null,
  clientId: "",
  idempotencyKey: crypto.randomUUID()
};

const $ = (id) => document.getElementById(id);

async function loadLookups() {
  const res = await fetch("/api/lookups");
  state.lookups = await res.json();
  fillSelect($("timekeeper"), state.lookups.timekeepers.map((t) => ({
    value: t.id,
    label: `${t.name} (${t.officeTimeZone})`
  })));
  $("timekeeper").value = "alice";
  const list = $("client-list");
  list.innerHTML = "";
  for (const c of state.lookups.clients) {
    const opt = document.createElement("option");
    opt.value = c.name;
    opt.dataset.id = c.id;
    list.appendChild(opt);
  }
  fillSelect($("activity-category"), state.lookups.categories.map((c) => ({ value: c, label: c })));
  refreshMatters();
  await refreshEntries();
}

function fillSelect(select, items) {
  select.innerHTML = items.map((i) => `<option value="${i.value}">${i.label}</option>`).join("");
}

function refreshMatters() {
  const select = $("matter-select");
  const matters = (state.lookups?.matters ?? []).filter((m) => m.clientId === state.clientId);
  if (!state.clientId || matters.length === 0) {
    select.disabled = true;
    select.innerHTML = `<option value="">Select a client first</option>`;
    return;
  }
  select.disabled = false;
  select.innerHTML = `<option value="">Select matter</option>` +
    matters.map((m) => `<option value="${m.id}">${m.name}</option>`).join("");
}

function chosenClient() {
  const typed = $("client-search").value.trim().toLowerCase();
  return (state.lookups?.clients ?? []).find((c) => c.name.toLowerCase() === typed || c.id === typed);
}

$("client-search").addEventListener("input", () => {
  const client = chosenClient();
  state.clientId = client ? client.id : "";
  $("client-chosen").textContent = client ? `Selected: ${client.name}` : "";
  refreshMatters();
});

$("entry-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const error = document.querySelector("[data-testid=form-error]");
  const save = $("save-entry");
  error.hidden = true;
  save.disabled = true;

  const pa = $("pa-mode").checked;
  const client = chosenClient();
  const body = {
    timekeeperId: $("timekeeper").value,
    clientId: client?.id ?? "",
    matterId: $("matter-select").value,
    durationHours: Number($("duration").value),
    activityCategory: $("activity-category").value,
    comment: $("narrative").value,
    enteredBy: pa ? "PA" : "Lawyer",
    source: "Ui",
    idempotencyKey: state.idempotencyKey
  };

  try {
    const res = await fetch("/api/entries?slow=1", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Idempotency-Key": state.idempotencyKey
      },
      body: JSON.stringify(body)
    });
    const data = await res.json();
    if (!res.ok) {
      error.hidden = false;
      error.textContent = data.error ?? "Could not save.";
      return;
    }
    state.idempotencyKey = crypto.randomUUID();
    $("narrative").value = "";
    await refreshEntries();
  } finally {
    save.disabled = false;
  }
});

async function refreshEntries() {
  const res = await fetch("/api/entries");
  const entries = await res.json();
  const list = $("todays-entries");
  list.innerHTML = entries.length === 0
    ? `<li class="entry">No entries yet.</li>`
    : entries.map((e) => `
        <li class="entry" data-testid="entry-${e.id}">
          <strong>${e.timekeeperName} · ${e.matterName} · ${e.durationHours}h</strong>
          <span>${e.activityCategory} · entered by ${e.enteredBy} · ${e.source}</span><br />
          <span>${e.comment}</span><br />
          <span>UTC ${e.createdAtUtc} · office ${e.officeTimeZone}</span>
        </li>`).join("");
}

loadLookups();
