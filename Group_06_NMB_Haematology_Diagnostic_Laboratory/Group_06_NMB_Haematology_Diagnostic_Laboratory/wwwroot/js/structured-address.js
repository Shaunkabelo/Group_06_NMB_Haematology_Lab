(() => {
    const endpoints = {};

    const byId = (prefix, name) => document.getElementById(`${prefix}-${name}`);
    const options = (select, placeholder, items, valueField, text) => {
        select.replaceChildren(new Option(placeholder, ''));
        items.forEach(item => {
            const option = new Option(text(item), item[valueField]);
            if (item.areaCode) option.dataset.areaCode = item.areaCode;
            select.add(option);
        });
        select.disabled = false;
    };
    const clearTown = prefix => {
        const town = byId(prefix, 'TownCity');
        const suburb = byId(prefix, 'Suburb');
        town.replaceChildren(new Option('Select a province first', ''));
        town.disabled = true;
        suburb.replaceChildren(new Option('Select a town / city first', ''));
        suburb.disabled = true;
        byId(prefix, 'AreaCode').value = '';
    };
    const clearSuburb = prefix => {
        const suburb = byId(prefix, 'Suburb');
        suburb.replaceChildren(new Option('Select a town / city first', ''));
        suburb.disabled = true;
        byId(prefix, 'AreaCode').value = '';
    };
    async function loadTowns(prefix, provinceId, selectedTownId) {
        clearTown(prefix);
        if (!provinceId) return;
        const response = await fetch(`${endpoints[prefix].towns}?provinceId=${encodeURIComponent(provinceId)}`);
        if (!response.ok) return;
        const towns = await response.json();
        options(byId(prefix, 'TownCity'), 'Select a town / city', towns, 'townCityId', town => town.townCityName);
        if (selectedTownId) {
            byId(prefix, 'TownCity').value = String(selectedTownId);
            await loadSuburbs(prefix, selectedTownId, null);
        }
    }
    async function loadSuburbs(prefix, townCityId, selectedSuburbId) {
        clearSuburb(prefix);
        if (!townCityId) return;
        const response = await fetch(`${endpoints[prefix].suburbs}?townCityId=${encodeURIComponent(townCityId)}`);
        if (!response.ok) return;
        const suburbs = await response.json();
        options(byId(prefix, 'Suburb'), 'Select a suburb', suburbs, 'suburbId', suburb => suburb.suburbName);
        if (selectedSuburbId) {
            byId(prefix, 'Suburb').value = String(selectedSuburbId);
            const selected = suburbs.find(suburb => String(suburb.suburbId) === String(selectedSuburbId));
            byId(prefix, 'AreaCode').value = selected?.areaCode || '';
        }
    }
    window.structuredAddress = {
        initialise(prefix) {
            const container = document.querySelector(`[data-address-prefix="${prefix}"]`);
            endpoints[prefix] = { towns: container.dataset.townsUrl, suburbs: container.dataset.suburbsUrl };
            byId(prefix, 'Province').addEventListener('change', event => loadTowns(prefix, event.target.value, null));
            byId(prefix, 'TownCity').addEventListener('change', event => loadSuburbs(prefix, event.target.value, null));
            byId(prefix, 'Suburb').addEventListener('change', event => {
                const option = event.target.selectedOptions[0];
                const suburb = option && [...event.target.options].find(item => item.value === option.value);
                byId(prefix, 'AreaCode').value = suburb?.dataset?.areaCode || '';
            });
        },
        reset(prefix) {
            byId(prefix, 'AddressLine1').value = '';
            byId(prefix, 'AddressLine2').value = '';
            byId(prefix, 'Province').value = '';
            clearTown(prefix);
        },
        async set(prefix, address) {
            byId(prefix, 'AddressLine1').value = address?.line1 || '';
            byId(prefix, 'AddressLine2').value = address?.line2 || '';
            byId(prefix, 'Province').value = address?.provinceId ? String(address.provinceId) : '';
            if (address?.provinceId) await loadTowns(prefix, address.provinceId, address.townCityId);
            if (address?.suburbId) await loadSuburbs(prefix, address.townCityId, address.suburbId);
        }
    };
    document.querySelectorAll('[data-structured-address]').forEach(element => window.structuredAddress.initialise(element.dataset.addressPrefix));
})();
