(function () {
  function byId(id) {
    return document.getElementById(id);
  }

  function setStatus(message, type) {
    const el = byId('status');
    if (!el) return;
    el.textContent = message;
    el.className = 'small status' + (type ? ' ' + type : '');
  }

  function getParams() {
    return new URLSearchParams(window.location.search);
  }

  function normalizePlaceId(value) {
    const text = String(value || '').trim();
    return /^\d+$/.test(text) ? text : '';
  }

  function normalizeInstanceId(value) {
    return String(value || '').trim();
  }

  function normalizeLaunchData(value) {
    return String(value || '').trim();
  }

  function normalizePrivateCode(value) {
    const text = String(value || '').trim();
    const match = text.match(/[?&]code=([A-Za-z0-9]+)/i);
    if (match) return match[1];
    const privateLinkCode = normalizePrivateLinkCode(text);
    return privateLinkCode || text.replace(/[^A-Za-z0-9]/g, '');
  }

  function normalizePrivateLinkCode(value) {
    const text = String(value || '').trim();
    const match = text.match(/[?&]privateServerLinkCode=([A-Za-z0-9]+)/i);
    return match ? match[1] : text.replace(/[^A-Za-z0-9]/g, '');
  }

  function parseInviteLikeInput(value) {
    let text = String(value || '').trim();
    if (!text) return null;
    // Tolerate links pasted without the scheme.
    if (!/^https?:\/\//i.test(text) && /^([a-z0-9-]+\.)*roblox\.com\//i.test(text)) {
      text = 'https://' + text;
    }
    try {
      const url = new URL(text);
      const params = url.searchParams;
      let placeId = normalizePlaceId(params.get('placeId'));
      // Normal game URLs carry the place ID in the path: /games/<id>/Game-Name
      if (!placeId) {
        const pathMatch = url.pathname.match(/\/games\/(\d+)/i);
        if (pathMatch) placeId = pathMatch[1];
      }
      return {
        placeId: placeId,
        instanceId: normalizeInstanceId(params.get('gameInstanceId') || params.get('jobId')),
        launchData: normalizeLaunchData(params.get('launchData')),
        privateCode: normalizePrivateCode(params.get('privateCode') || params.get('linkCode') || params.get('privateServerCode') || params.get('privateServerLink') || params.get('code') || params.get('privateServerLinkCode'))
      };
    } catch (_) {
      return null;
    }
  }

  function buildDeepLink(placeId, instanceId) {
    return 'roblox://placeId=' + encodeURIComponent(placeId) + '&gameInstanceId=' + encodeURIComponent(instanceId);
  }

  function buildPlaceLink(placeId) {
    return 'roblox://placeId=' + encodeURIComponent(placeId);
  }

  function buildLaunchDataLink(placeId, launchData, instanceId) {
    let link = 'roblox://placeId=' + encodeURIComponent(placeId);
    if (instanceId) {
      link += '&gameInstanceId=' + encodeURIComponent(instanceId);
    }
    if (launchData) {
      link += '&launchData=' + encodeURIComponent(launchData);
    }
    return link;
  }

  function buildLaunchDataWebLink(placeId, launchData, instanceId) {
    let link = 'https://www.roblox.com/games/start?placeId=' + encodeURIComponent(placeId);
    if (instanceId) {
      link += '&gameInstanceId=' + encodeURIComponent(instanceId);
    }
    if (launchData) {
      link += '&launchData=' + encodeURIComponent(launchData);
    }
    return link;
  }

  function buildPrivateLink(placeId, privateCode) {
    if (!privateCode) return '';
    return placeId
      ? 'roblox://placeId=' + encodeURIComponent(placeId) + '&linkCode=' + encodeURIComponent(privateCode)
      : 'roblox://navigation/share_links?code=' + encodeURIComponent(privateCode) + '&type=Server';
  }

  function buildWebLink(placeId, instanceId) {
    return 'https://www.roblox.com/games/' + encodeURIComponent(placeId) + '/?gameInstanceId=' + encodeURIComponent(instanceId);
  }

  function buildPlaceWebLink(placeId) {
    return 'https://www.roblox.com/games/' + encodeURIComponent(placeId) + '/';
  }

  function buildPrivateWebLink(privateCode) {
    return 'https://www.roblox.com/share?code=' + encodeURIComponent(privateCode) + '&type=Server';
  }

  function buildShareLinkDeepLink(privateCode) {
    return 'roblox://navigation/share_links?code=' + encodeURIComponent(privateCode) + '&type=Server';
  }

  function buildCommunityIssueUrl(payload) {
    const url = new URL('https://github.com/Arctic-Furry/arctic-furry.github.io/issues/new');
    url.searchParams.set('title', 'Community private server: ' + payload.gameName);
    url.searchParams.set('body', [
      '## Community Private Server Submission',
      '',
      '**Submitted by:** ' + payload.submitterLabel,
      '**Avatar:** ' + payload.submitterAvatar,
      '**Game name:** ' + payload.gameName,
      '**Category:** ' + payload.categoryLabel,
      '**Private server share link:** ' + payload.shareLink,
      '**Share code:** ' + payload.shareCode,
      payload.note ? '**Note:** ' + payload.note : '**Note:** (none)',
      '',
      '> This was submitted from the /ps community upload form and should only be added after review.'
    ].join('\n'));
    return url.toString();
  }

  function getDiscordAvatarUrl(user) {
    if (!user) return '';
    if (user.avatar) {
      return 'https://cdn.discordapp.com/avatars/' + user.id + '/' + user.avatar + '.png?size=128';
    }
    const discriminator = Number(user.discriminator || '0');
    const fallback = Number.isFinite(discriminator) ? discriminator % 5 : 0;
    return 'https://cdn.discordapp.com/embed/avatars/' + fallback + '.png';
  }

  function makeDiscordAuthUrl() {
    const url = new URL('https://discord.com/oauth2/authorize');
    url.searchParams.set('client_id', '1521410277600661554');
    url.searchParams.set('response_type', 'token');
    url.searchParams.set('redirect_uri', 'https://arctic-furry.github.io/ps/');
    url.searchParams.set('scope', 'identify');
    url.searchParams.set('prompt', 'consent');
    return url.toString();
  }

  const GITHUB_OWNER = 'Arctic-Furry';
  const GITHUB_REPO = 'arctic-furry.github.io';
  const COMMUNITY_POSTS_PATH = 'data/community-posts.json';
  const CHANGELOGS_PATH = 'data/changelogs.json';

  function isCommunityAdmin(user) {
    return !!(user && user.id === '1290683047921979393');
  }

  function getCacheBustedPath(path) {
    return path + '?v=' + Date.now();
  }

  async function fetchJsonFile(path) {
    const response = await fetch(getCacheBustedPath('../' + path).replace('/../', '/'), { cache: 'no-store' });
    if (!response.ok) {
      throw new Error('Could not load ' + path);
    }
    return response.json();
  }

  function encodeBase64Unicode(value) {
    return btoa(unescape(encodeURIComponent(value)));
  }

  async function readGitHubContents(path, token) {
    const response = await fetch('https://api.github.com/repos/' + GITHUB_OWNER + '/' + GITHUB_REPO + '/contents/' + path, {
      headers: {
        Authorization: 'Bearer ' + token,
        Accept: 'application/vnd.github+json'
      }
    });
    if (!response.ok) {
      throw new Error('GitHub read failed for ' + path);
    }
    return response.json();
  }

  async function writeGitHubJson(path, jsonValue, message, token) {
    const current = await readGitHubContents(path, token);
    const response = await fetch('https://api.github.com/repos/' + GITHUB_OWNER + '/' + GITHUB_REPO + '/contents/' + path, {
      method: 'PUT',
      headers: {
        Authorization: 'Bearer ' + token,
        Accept: 'application/vnd.github+json',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        message: message,
        content: encodeBase64Unicode(JSON.stringify(jsonValue, null, 2) + '\n'),
        sha: current.sha
      })
    });
    if (!response.ok) {
      throw new Error('GitHub write failed for ' + path);
    }
    return response.json();
  }

  async function fetchGitHubIssues(token) {
    const response = await fetch('https://api.github.com/repos/' + GITHUB_OWNER + '/' + GITHUB_REPO + '/issues?state=open&per_page=100', {
      headers: {
        Authorization: 'Bearer ' + token,
        Accept: 'application/vnd.github+json'
      }
    });
    if (!response.ok) {
      throw new Error('GitHub issue fetch failed');
    }
    return response.json();
  }

  async function closeGitHubIssue(issueNumber, token, stateReason) {
    const response = await fetch('https://api.github.com/repos/' + GITHUB_OWNER + '/' + GITHUB_REPO + '/issues/' + issueNumber, {
      method: 'PATCH',
      headers: {
        Authorization: 'Bearer ' + token,
        Accept: 'application/vnd.github+json',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ state: 'closed', state_reason: stateReason || 'completed' })
    });
    if (!response.ok) {
      throw new Error('Could not close issue #' + issueNumber);
    }
    return response.json();
  }

  function extractIssueField(body, label) {
    const pattern = new RegExp('\\*\\*' + label.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ':\\*\\*\\s*(.+)', 'i');
    const match = String(body || '').match(pattern);
    return match ? match[1].trim() : '';
  }

  function parseCommunityIssue(issue) {
    const body = String(issue && issue.body || '');
    const title = String(issue && issue.title || '');
    if (!/^Community private server:/i.test(title)) return null;
    const submitterLabel = extractIssueField(body, 'Submitted by');
    const gameName = extractIssueField(body, 'Game name');
    const categoryLabel = extractIssueField(body, 'Category') || 'None';
      const avatar = extractIssueField(body, 'Avatar') || String(issue && issue.user && issue.user.avatar_url || '');
      const shareLink = extractIssueField(body, 'Private server share link');
    const shareCode = extractIssueField(body, 'Share code');
    const note = extractIssueField(body, 'Note').replace(/^\(none\)$/i, '').trim();
    const submitterMatch = submitterLabel.match(/^(.+?)\s*\((\d+)\)$/);
    return {
      issueNumber: issue.number,
      submitterLabel: submitterLabel,
      submitterId: submitterMatch ? submitterMatch[2] : '',
      submitterName: submitterMatch ? submitterMatch[1] : submitterLabel,
      gameName: gameName,
      categoryLabel: categoryLabel,
      category: categoryLabel.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || 'none',
      shareLink: shareLink,
      shareCode: shareCode,
        note: note,
        submitterAvatar: avatar
    };
  }

  function buildDeleteRequestIssueUrl(post) {
    const url = new URL('https://github.com/' + GITHUB_OWNER + '/' + GITHUB_REPO + '/issues/new');
    url.searchParams.set('title', 'Delete community post: ' + post.gameName);
    url.searchParams.set('body', [
      '## Community Post Delete Request',
      '',
      '**Post ID:** ' + post.id,
      '**Submitter:** ' + post.submitterLabel,
      '**Game:** ' + post.gameName,
      '',
      '> Requested from the /ps page by the verified submitter.'
    ].join('\n'));
    return url.toString();
  }

  function escapeHtml(value) {
    return String(value || '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  async function copyText(text, successLabel) {
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(text);
      } else {
        const temp = document.createElement('textarea');
        temp.value = text;
        document.body.appendChild(temp);
        temp.select();
        document.execCommand('copy');
        document.body.removeChild(temp);
      }
      setStatus(successLabel || 'Copied.', 'ok');
      return true;
    } catch (error) {
      setStatus('Could not copy automatically.', 'error');
      return false;
    }
  }

  function initInvitePage() {
    const params = getParams();
    const placeId = normalizePlaceId(params.get('placeId'));
    const instanceId = normalizeInstanceId(params.get('gameInstanceId') || params.get('jobId'));
    const privateCode = normalizePrivateCode(params.get('privateCode') || params.get('linkCode') || params.get('privateServerCode') || params.get('privateServerLink') || params.get('privateServerLinkCode'));
    const launchData = normalizeLaunchData(params.get('launchData'));
    const mode = privateCode ? 'private' : 'public';
    const jobValue = byId('job-id');
    const placeBlock = byId('place-block');
    const placeValue = byId('place-id');
    const placeLabel = byId('place-label');
    const jobLabel = byId('job-label');
    const openApp = byId('open-app');
    const openWeb = byId('open-web');
    const description = byId('invite-description');
    const deepLink = mode === 'private'
      ? buildPrivateLink(placeId, privateCode)
      : launchData
        ? buildLaunchDataLink(placeId, launchData, instanceId)
        : instanceId
          ? buildDeepLink(placeId, instanceId)
          : buildPlaceLink(placeId);
    const webLink = mode === 'private'
      ? buildPrivateWebLink(privateCode)
      : launchData
        ? buildLaunchDataWebLink(placeId, launchData, instanceId)
        : instanceId
          ? buildWebLink(placeId, instanceId)
          : buildPlaceWebLink(placeId);

    if (!placeId && mode !== 'private') {
      window.location.replace('../menu/' + window.location.search + window.location.hash);
      return;
    }

    if (mode === 'private' && !privateCode) {
      window.location.replace('../menu/' + window.location.search + window.location.hash);
      return;
    }

    if (placeLabel) {
      placeLabel.textContent = mode === 'private' ? 'Private Server' : 'Place ID';
    }
    if (placeBlock) {
      placeBlock.classList.toggle('hidden', mode === 'private');
    }
    if (placeValue) {
      placeValue.textContent = placeId;
    }
    if (placeId && mode !== 'private') {
      // Fetch the game's name + icon so the invite page shows what you are joining.
      fetch('https://games.roproxy.com/v1/games/multiget-place-details?placeIds=' + placeId)
        .then(function (r) { return r.json(); })
        .then(function (games) {
          if (!games || !games.length) return;
          var game = games[0];
          if (placeLabel) placeLabel.textContent = game.Name || 'Place ID';
          if (description) {
            description.textContent = 'Joining ' + (game.Name || 'this place') +
              '. Tap the button below to open Roblox for this place' +
              (launchData || instanceId ? ' with its exact details' : '') +
              '. If Roblox does not launch, use the fallback web button.';
          }
        })
        .catch(function () {});
    }
    if (openWeb) openWeb.href = webLink;
    if (description) {
      description.textContent = mode === 'private'
        ? 'Tap the button below to open Roblox for this private server. If the app does not launch, use the fallback web button.'
        : launchData
          ? 'Tap the button below to open Roblox for this place with its launch data. If the app does not launch, use the fallback web button.'
          : instanceId
            ? 'Tap the button below to open the Roblox app for this exact server on desktop or mobile. If Roblox does not launch, use the fallback web button.'
            : 'Tap the button below to open Roblox for this place. If Roblox does not launch, use the fallback web button.';
    }
    if (jobLabel) {
      jobLabel.textContent = mode === 'private'
        ? 'Private Server Code'
        : launchData
          ? 'Launch Data'
          : instanceId
            ? 'Server Job ID'
            : 'Join Type';
    }
    if (jobValue) {
      jobValue.textContent = mode === 'private'
        ? privateCode
        : launchData || instanceId || 'Public place join';
    }

    function openRoblox() {
      window.location.href = deepLink;
    }

    const autoJoinToggle = byId('auto-join-toggle');
    let autoJoinEnabled = false;
    try { autoJoinEnabled = localStorage.getItem('joiner.autoJoin') === '1'; } catch (_) {}

    if (autoJoinToggle) {
      autoJoinToggle.checked = autoJoinEnabled;
      autoJoinToggle.addEventListener('change', function () {
        try { localStorage.setItem('joiner.autoJoin', autoJoinToggle.checked ? '1' : '0'); } catch (_) {}
        setStatus(autoJoinToggle.checked
          ? 'Auto-Join is ON: future join links will open Roblox instantly.'
          : 'Auto-Join is OFF: you pick App or Web yourself each time.');
      });
    }

    openApp && openApp.addEventListener('click', openRoblox);

    if (autoJoinEnabled) {
      setStatus('Auto-Join is ON: opening Roblox…');
      window.setTimeout(openRoblox, 250);
    } else {
      setStatus('Pick how you want to join: Open Roblox App or Open Roblox Web Page.');
    }
  }

  function initMenuPage() {
    const modeInputs = document.querySelectorAll('input[name="joinType"]');
    const placeField = byId('place-field');
    const inviteLinkField = byId('invite-link-field');
    const placeInput = byId('placeId');
    const instanceInput = byId('gameInstanceId');
    const inviteLinkInput = byId('inviteLink');
    const launchDataInput = byId('launchData');
    const privateInput = byId('privateCode');
    const privateGameLinkInput = byId('privateGameLink');
    const publicFields = byId('public-fields');
    const privateFields = byId('private-fields');
    const launchButton = byId('launch-invite');
    const viewGameButton = byId('view-game');
    const copyLinkButton = byId('copy-link');
    const output = byId('generated-link');
    const advancedToggle = byId('advanced-toggle');
    const advancedPanel = byId('advanced-panel');
    let lastInviteLinkApplied = '';
    let lastPublicPlaceId = '';

    advancedToggle && advancedToggle.addEventListener('click', function () {
      const open = advancedPanel.classList.toggle('hidden');
      advancedToggle.textContent = open ? 'Advanced' : 'Hide Advanced';
    });

    function getMode() {
      const checked = document.querySelector('input[name="joinType"]:checked');
      return checked ? checked.value : 'public';
    }

    function makeUrl() {
      const mode = getMode();
      const placeId = normalizePlaceId(placeInput && placeInput.value);
      const instanceId = normalizeInstanceId(instanceInput && instanceInput.value);
      const launchData = normalizeLaunchData(launchDataInput && launchDataInput.value);
      const privateCode = normalizePrivateCode(
        (privateInput && privateInput.value) || (privateGameLinkInput && privateGameLinkInput.value)
      );
      const url = new URL('../invite/', window.location.href);
      if (mode === 'private') {
        if (!privateCode) return '';
        url.searchParams.set('privateCode', privateCode);
        return url.toString();
      }
      if (!placeId) return '';
      url.searchParams.set('placeId', placeId);
      if (instanceId) {
        url.searchParams.set('gameInstanceId', instanceId);
      }
      if (launchData) {
        url.searchParams.set('launchData', launchData);
      }
      return url.toString();
    }

    function makeViewUrl() {
      const mode = getMode();
      const placeId = normalizePlaceId(placeInput && placeInput.value);
      const instanceId = normalizeInstanceId(instanceInput && instanceInput.value);
      if (mode === 'private') {
        return lastPublicPlaceId ? buildPlaceWebLink(lastPublicPlaceId) : '';
      }
      if (!placeId) return '';
      return instanceId ? buildWebLink(placeId, instanceId) : buildPlaceWebLink(placeId);
    }

    function syncModeUi() {
      const mode = getMode();
      if (placeField) placeField.classList.toggle('hidden', mode === 'private');
      if (inviteLinkField) inviteLinkField.classList.toggle('hidden', mode !== 'public');
      if (publicFields) publicFields.classList.toggle('hidden', mode !== 'public');
      if (privateFields) privateFields.classList.toggle('hidden', mode !== 'private');
    }

    function absorbInviteLink() {
      const raw = String(inviteLinkInput && inviteLinkInput.value || '').trim();
      if (!raw) {
        if (lastInviteLinkApplied) {
          lastInviteLinkApplied = '';
          if (placeInput) placeInput.value = '';
          if (instanceInput) instanceInput.value = '';
          if (launchDataInput) launchDataInput.value = '';
        }
        return;
      }
      if (raw === lastInviteLinkApplied) return;
      const parsed = parseInviteLikeInput(raw);
      if (!parsed) return;
      lastInviteLinkApplied = raw;
      if (parsed.privateCode) {
        const privateRadio = document.querySelector('input[name="joinType"][value="private"]');
        if (privateRadio) privateRadio.checked = true;
        if (privateInput) privateInput.value = parsed.privateCode;
        if (privateGameLinkInput) privateGameLinkInput.value = raw;
        if (parsed.placeId) lastPublicPlaceId = parsed.placeId;
        if (placeInput) placeInput.value = '';
        if (instanceInput) instanceInput.value = '';
        return;
      }
      if (parsed.placeId || parsed.instanceId || parsed.launchData) {
        const publicRadio = document.querySelector('input[name="joinType"][value="public"]');
        if (publicRadio) publicRadio.checked = true;
        if (placeInput && parsed.placeId) placeInput.value = parsed.placeId;
        if (instanceInput && parsed.instanceId) instanceInput.value = parsed.instanceId;
        if (launchDataInput && parsed.launchData) launchDataInput.value = parsed.launchData;
      }
    }

    function loadAutoJoinPref() {
      try { return localStorage.getItem('joiner.autoJoin') === '1'; } catch (_) { return false; }
    }

    function saveAutoJoinPref(enabled) {
      try { localStorage.setItem('joiner.autoJoin', enabled ? '1' : '0'); } catch (_) {}
    }

    function buildDirectDeepLink() {
      const mode = getMode();
      if (mode === 'private') {
        const privateCode = normalizePrivateCode(
          (privateInput && privateInput.value) || (privateGameLinkInput && privateGameLinkInput.value)
        );
        return buildShareLinkDeepLink(privateCode);
      }
      const placeId = normalizePlaceId(placeInput && placeInput.value);
      const instanceId = normalizeInstanceId(instanceInput && instanceInput.value);
      const launchData = normalizeLaunchData(launchDataInput && launchDataInput.value);
      if (launchData) {
        return buildLaunchDataLink(placeId, launchData, instanceId);
      }
      return instanceId ? buildDeepLink(placeId, instanceId) : buildPlaceLink(placeId);
    }

    function refresh() {
      syncModeUi();
      const mode = getMode();
      if (mode === 'public') {
        const currentPublicPlaceId = normalizePlaceId(placeInput && placeInput.value);
        if (currentPublicPlaceId) {
          lastPublicPlaceId = currentPublicPlaceId;
        }
      }
      const url = makeUrl();
      const viewUrl = makeViewUrl();
      output.textContent = url || 'Fill in the required fields to generate a join link.';
      if (launchButton) launchButton.disabled = !url;
      if (viewGameButton) viewGameButton.disabled = !viewUrl;
      if (copyLinkButton) copyLinkButton.disabled = !url;
      setStatus(
        url
          ? (getMode() === 'private' ? 'Private server join link ready.' : 'Public join link ready.')
          : 'Paste any Roblox link above — public or private, it is detected automatically. Or open Advanced to enter details yourself.'
      );
    }

    modeInputs.forEach(function (input) {
      input.addEventListener('change', refresh);
    });
    inviteLinkInput && inviteLinkInput.addEventListener('input', function () {
      absorbInviteLink();
      refresh();
    });
    placeInput && placeInput.addEventListener('input', refresh);
    instanceInput && instanceInput.addEventListener('input', refresh);
    launchDataInput && launchDataInput.addEventListener('input', refresh);
    privateInput && privateInput.addEventListener('input', refresh);
    privateGameLinkInput && privateGameLinkInput.addEventListener('input', refresh);

    launchButton && launchButton.addEventListener('click', function () {
      const url = makeUrl();
      if (!url) return;
      const autoJoinNow = byId('auto-join-toggle');
      if (autoJoinNow && autoJoinNow.checked) {
        const deepLink = buildDirectDeepLink();
        if (deepLink) {
          window.location.href = deepLink;
          return;
        }
      }
      window.location.href = url;
    });

    viewGameButton && viewGameButton.addEventListener('click', function () {
      const viewUrl = makeViewUrl();
      if (viewUrl) {
        window.open(viewUrl, '_blank', 'noopener');
      }
    });

    copyLinkButton && copyLinkButton.addEventListener('click', function () {
      const url = makeUrl();
      if (url) copyText(url, 'Join link copied.');
    });

    const autoJoinCheckbox = byId('auto-join-toggle');
    if (autoJoinCheckbox) {
      autoJoinCheckbox.checked = loadAutoJoinPref();
      autoJoinCheckbox.addEventListener('change', function () {
        saveAutoJoinPref(autoJoinCheckbox.checked);
        setStatus(autoJoinCheckbox.checked
          ? 'Auto-Join is ON: pressing Join Server opens Roblox instantly.'
          : 'Auto-Join is OFF: Join Server shows the App/Browser choice page.');
      });
    }

    refresh();
  }

  function initPrivateServersPage() {
    const grid = byId('ps-grid');
    const cards = Array.from(document.querySelectorAll('[data-ps-card]'));
    const searchInput = byId('ps-search');
    const categorySelect = byId('ps-category');
    const sortSelect = byId('ps-sort');
    const emptyState = byId('ps-empty');
    const buttons = document.querySelectorAll('[data-share-code]');
    const communityName = byId('community-game-name');
    const communityCategory = byId('community-category');
    const communityLink = byId('community-private-link');
    const communityNote = byId('community-note');
    const communityPreview = byId('community-preview');
    const communityStatus = byId('community-status');
    const communitySubmit = byId('community-submit');
    const toggleCommunityForm = byId('toggle-community-form');
    const communityFormPanel = byId('community-form-panel');
    const toggleAdminMenu = byId('toggle-admin-menu');
    const discordLogin = byId('discord-login');
    const discordLogout = byId('discord-logout');
    const verifiedUser = byId('verified-user');
    const verifiedAvatar = byId('verified-avatar');
    const verifiedName = byId('verified-name');
    const verifiedMeta = byId('verified-meta');
    const communityPosts = byId('community-posts');
    const communityEmpty = byId('community-empty');
    const adminPanel = byId('admin-panel');
    const adminTokenInput = byId('admin-github-token');
    const adminSaveToken = byId('admin-save-token');
    const adminClearToken = byId('admin-clear-token');
    const adminPublishPost = byId('admin-publish-post');
    const adminDeletePost = byId('admin-delete-post');
    const adminDeleteNameInput = byId('admin-delete-name');
    const adminStatus = byId('admin-status');
    const reviewQueue = byId('review-queue');
    const reviewEmpty = byId('review-empty');
    let verifiedDiscordUser = null;
    let globalCommunityPosts = [];
    let pendingReviewIssues = [];
    let communityFormOpen = false;
    let adminMenuOpen = false;
    buttons.forEach(function (button) {
      button.addEventListener('click', function () {
        const code = button.getAttribute('data-share-code') || '';
        if (!code) return;
        window.location.href = buildShareLinkDeepLink(code);
      });
    });

    document.querySelectorAll('[data-ps-card] .game-actions').forEach(function (actions) {
      const joinButton = actions.querySelector('[data-share-code]');
      if (!joinButton || actions.querySelector('[data-copy-static]')) return;
      const staticCopyButton = document.createElement('button');
      staticCopyButton.type = 'button';
      staticCopyButton.className = 'button secondary hidden';
      staticCopyButton.setAttribute('data-copy-static', joinButton.getAttribute('data-share-code') || '');
      staticCopyButton.textContent = 'Copy Join Link';
      actions.appendChild(staticCopyButton);
    });
    document.querySelectorAll('[data-copy-static]').forEach(function (button) {
      button.addEventListener('click', function () {
        const code = button.getAttribute('data-copy-static') || '';
        if (!code) return;
        copyText(buildPrivateWebLink(code)).then(function (ok) {
          setCommunityStatus(ok ? 'Link copied to your clipboard.' : 'Could not copy the link.', ok ? 'ok' : 'error');
        });
      });
    });

    function setCommunityStatus(message, type) {
      if (!communityStatus) return;
      communityStatus.textContent = message;
      communityStatus.className = 'small status' + (type ? ' ' + type : '');
    }

    function setAdminStatus(message, type) {
      if (!adminStatus) return;
      adminStatus.textContent = message;
      adminStatus.className = 'small status' + (type ? ' ' + type : '');
    }

    function getCategoryLabel(value) {
      const option = communityCategory && communityCategory.options[communityCategory.selectedIndex];
      return option ? option.text : value;
    }

    function persistVerifiedUser(user) {
      try {
        localStorage.setItem('ps.discordUser', JSON.stringify(user));
      } catch (_) {}
    }

    function persistAdminToken(token) {
      try {
        localStorage.setItem('ps.githubToken', token);
      } catch (_) {}
    }

    function loadAdminToken() {
      try {
        return localStorage.getItem('ps.githubToken') || '';
      } catch (_) {
        return '';
      }
    }

    function clearAdminToken() {
      try {
        localStorage.removeItem('ps.githubToken');
      } catch (_) {}
    }

    function persistDraft() {
      try {
        localStorage.setItem('ps.communityDraft', JSON.stringify({
          gameName: String(communityName && communityName.value || ''),
          category: String(communityCategory && communityCategory.value || 'none'),
          shareLink: String(communityLink && communityLink.value || ''),
          note: String(communityNote && communityNote.value || '')
        }));
      } catch (_) {}
    }

    function loadDraft() {
      try {
        const raw = localStorage.getItem('ps.communityDraft');
        return raw ? JSON.parse(raw) : null;
      } catch (_) {
        return null;
      }
    }

    function restoreDraft() {
      const draft = loadDraft();
      if (!draft) return;
      if (communityName && draft.gameName) communityName.value = draft.gameName;
      if (communityCategory && draft.category) communityCategory.value = draft.category;
      if (communityLink && draft.shareLink) communityLink.value = draft.shareLink;
      if (communityNote && draft.note) communityNote.value = draft.note;
    }

    function clearDraft() {
      try {
        localStorage.removeItem('ps.communityDraft');
      } catch (_) {}
    }

    function clearVerifiedUser() {
      verifiedDiscordUser = null;
      try {
        localStorage.removeItem('ps.discordUser');
      } catch (_) {}
    }

    function loadStoredVerifiedUser() {
      try {
        const raw = localStorage.getItem('ps.discordUser');
        return raw ? JSON.parse(raw) : null;
      } catch (_) {
        return null;
      }
    }

    function loadCommunityPosts() {
      return Array.isArray(globalCommunityPosts) ? globalCommunityPosts : [];
    }

    function saveCommunityPost(post) {
      globalCommunityPosts = [post].concat(loadCommunityPosts()).slice(0, 100);
    }

    function deleteCommunityPost(postId) {
      globalCommunityPosts = loadCommunityPosts().filter(function (post) {
        return post.id !== postId;
      });
    }

    function canDeleteCommunityPost(post) {
      if (!verifiedDiscordUser || !post) return false;
      return verifiedDiscordUser.id === post.submitterId || isCommunityAdmin(verifiedDiscordUser);
    }

    function canHardDeleteCommunityPost(post) {
      return !!(post && verifiedDiscordUser && isCommunityAdmin(verifiedDiscordUser));
    }

    function renderCommunityFormState() {
      if (!toggleCommunityForm || !communityFormPanel) return;
      communityFormPanel.classList.toggle('hidden', !communityFormOpen);
      toggleCommunityForm.textContent = communityFormOpen ? 'Hide Post Menu' : 'Open Post Menu';
    }

    function renderAdminPanel() {
      if (!adminPanel) return;
      const show = isCommunityAdmin(verifiedDiscordUser);
      adminPanel.classList.toggle('hidden', !show || !adminMenuOpen);
      if (toggleAdminMenu) {
        toggleAdminMenu.classList.toggle('hidden', !show);
        toggleAdminMenu.textContent = adminMenuOpen ? 'Hide Admin Menu' : 'Open Admin Menu';
      }
      if (show && adminTokenInput && !adminTokenInput.value) {
        adminTokenInput.value = loadAdminToken();
      }
    }

    function renderReviewQueue() {
      if (!reviewQueue) return;
      reviewQueue.innerHTML = pendingReviewIssues.map(function (entry) {
        return '<article class="game-card review-card">'
          + '<div class="game-meta"><span class="pill">Issue #' + escapeHtml(entry.issueNumber) + '</span><span class="pill">' + escapeHtml(entry.categoryLabel) + '</span></div>'
          + '<h2>' + escapeHtml(entry.gameName || 'Unnamed Submission') + '</h2>'
          + '<p><strong>' + escapeHtml(entry.submitterLabel || 'Unknown submitter') + '</strong></p>'
          + '<p>' + escapeHtml(entry.note || 'No note provided.') + '</p>'
          + '<div class="code-block">' + escapeHtml(entry.shareLink || '') + '</div>'
          + '<div class="game-actions">'
          + '<button class="button" data-approve-issue="' + escapeHtml(entry.issueNumber) + '">Approve to Community</button>'
          + '<button class="button secondary" data-reject-issue="' + escapeHtml(entry.issueNumber) + '">Reject</button>'
          + '<a class="button secondary" href="https://github.com/' + GITHUB_OWNER + '/' + GITHUB_REPO + '/issues/' + escapeHtml(entry.issueNumber) + '" target="_blank" rel="noopener">Open Issue</a>'
          + '</div>'
          + '</article>';
      }).join('');
      const approveButtons = reviewQueue.querySelectorAll('[data-approve-issue]');
      approveButtons.forEach(function (button) {
        button.addEventListener('click', async function () {
          const issueNumber = Number(button.getAttribute('data-approve-issue') || '0');
          if (!issueNumber) return;
          const token = String(adminTokenInput && adminTokenInput.value || loadAdminToken()).trim();
          if (!token) {
            setAdminStatus('Paste and save a GitHub token first.', 'error');
            return;
          }
          const issue = pendingReviewIssues.find(function (entry) { return entry.issueNumber === issueNumber; });
          if (!issue) return;
          try {
            const nextPosts = [{
              id: 'issue-' + issue.issueNumber + '-' + Date.now(),
              submitterId: issue.submitterId,
              submitterLabel: issue.submitterLabel,
              submitterAvatar: issue.submitterAvatar,
              gameName: issue.gameName,
              categoryLabel: issue.categoryLabel,
              category: issue.category,
              shareCode: issue.shareCode,
              shareLink: issue.shareLink,
              note: issue.note
            }].concat(loadCommunityPosts()).slice(0, 100);
            await writeGitHubJson(COMMUNITY_POSTS_PATH, nextPosts, 'Approve community post from issue #' + issue.issueNumber, token);
            await closeGitHubIssue(issue.issueNumber, token, 'completed');
            persistAdminToken(token);
            await refreshGlobalCommunityPosts();
            await refreshReviewQueue();
            setAdminStatus('Approved issue #' + issue.issueNumber + ' into Community Posts.', 'ok');
          } catch (_) {
            setAdminStatus('Could not approve issue #' + issue.issueNumber + '.', 'error');
          }
        });
      });
      const rejectButtons = reviewQueue.querySelectorAll('[data-reject-issue]');
      rejectButtons.forEach(function (button) {
        button.addEventListener('click', async function () {
          const issueNumber = Number(button.getAttribute('data-reject-issue') || '0');
          const token = String(adminTokenInput && adminTokenInput.value || loadAdminToken()).trim();
          if (!issueNumber || !token) {
            setAdminStatus('Save a GitHub token first.', 'error');
            return;
          }
          try {
            await closeGitHubIssue(issueNumber, token, 'not_planned');
            await refreshReviewQueue();
            setAdminStatus('Rejected issue #' + issueNumber + ' as not planned.', 'ok');
          } catch (_) {
            setAdminStatus('Could not reject issue #' + issueNumber + '.', 'error');
          }
        });
      });
      if (reviewEmpty) reviewEmpty.classList.toggle('hidden', pendingReviewIssues.length > 0);
    }

    async function refreshReviewQueue() {
      if (!isCommunityAdmin(verifiedDiscordUser)) {
        pendingReviewIssues = [];
        renderReviewQueue();
        return;
      }
      const token = String(adminTokenInput && adminTokenInput.value || loadAdminToken()).trim();
      if (!token) {
        pendingReviewIssues = [];
        renderReviewQueue();
        return;
      }
      try {
        const issues = await fetchGitHubIssues(token);
        pendingReviewIssues = issues
          .filter(function (issue) { return !issue.pull_request; })
          .map(parseCommunityIssue)
          .filter(Boolean);
      } catch (_) {
        pendingReviewIssues = [];
      }
      renderReviewQueue();
    }

    async function refreshGlobalCommunityPosts() {
      try {
        globalCommunityPosts = await fetchJsonFile('data/community-posts.json');
        if (!Array.isArray(globalCommunityPosts)) globalCommunityPosts = [];
        renderCommunityPosts();
      } catch (_) {
        globalCommunityPosts = [];
        renderCommunityPosts();
      }
    }

    function renderCommunityPosts() {
      if (!communityPosts) return;
      const posts = loadCommunityPosts();
      communityPosts.innerHTML = posts.map(function (post) {
        const deleteButton = canDeleteCommunityPost(post)
          ? '<button class="button secondary" data-delete-post="' + escapeHtml(post.id) + '" data-delete-mode="' + (canHardDeleteCommunityPost(post) ? 'hard' : 'request') + '">' + (canHardDeleteCommunityPost(post) ? 'Delete Post' : 'Request Delete') + '</button>'
          : '';
        const copyButton = verifiedDiscordUser
          ? '<button class="button secondary" data-copy-post="' + escapeHtml(post.shareLink) + '" type="button">Copy Join Link</button>'
          : '';
        return '<article class="game-card">'
          + '<div class="game-meta"><span class="pill">Community</span><span class="pill">' + escapeHtml(post.categoryLabel) + '</span></div>'
          + '<div class="verified-user" style="margin-top:0;padding-top:0;border-top:none;">'
          + '<img class="verified-avatar" src="' + escapeHtml(post.submitterAvatar) + '" alt="Submitter avatar" />'
          + '<div><strong>' + escapeHtml(post.submitterLabel) + '</strong><div class="small">Verified Discord submitter</div></div>'
          + '</div>'
          + '<h2>' + escapeHtml(post.gameName) + '</h2>'
          + '<p>' + escapeHtml(post.note || 'Community-submitted private server.') + '</p>'
          + '<div class="game-actions">'
          + '<button class="button" data-share-code="' + escapeHtml(post.shareCode) + '">Join in Roblox</button>'
          + '<a class="button secondary" href="' + escapeHtml(post.shareLink) + '">Open Share Link</a>'
          + copyButton
          + deleteButton
          + '</div>'
          + '</article>';
      }).join('');
      const postButtons = communityPosts.querySelectorAll('[data-share-code]');
      postButtons.forEach(function (button) {
        button.addEventListener('click', function () {
          const code = button.getAttribute('data-share-code') || '';
          if (!code) return;
          window.location.href = buildShareLinkDeepLink(code);
        });
      });
      const copyPostButtons = communityPosts.querySelectorAll('[data-copy-post]');
      copyPostButtons.forEach(function (button) {
        button.addEventListener('click', function () {
          const link = button.getAttribute('data-copy-post') || '';
          if (!link) return;
          copyText(link).then(function (ok) {
            setCommunityStatus(ok ? 'Link copied to your clipboard.' : 'Could not copy the link.', ok ? 'ok' : 'error');
          });
        });
      });
      const deleteButtons = communityPosts.querySelectorAll('[data-delete-post]');
      deleteButtons.forEach(function (button) {
        button.addEventListener('click', async function () {
          const postId = button.getAttribute('data-delete-post') || '';
          const mode = button.getAttribute('data-delete-mode') || 'request';
          if (!postId) return;
          const post = loadCommunityPosts().find(function (entry) { return entry.id === postId; });
          if (!post) return;
          if (mode === 'hard' && isCommunityAdmin(verifiedDiscordUser)) {
            const token = loadAdminToken();
            if (!token) {
              setCommunityStatus('Save a GitHub token in the admin menu before deleting global posts.', 'error');
              return;
            }
            try {
              deleteCommunityPost(postId);
              await writeGitHubJson(COMMUNITY_POSTS_PATH, loadCommunityPosts(), 'Delete community post ' + post.gameName, token);
              await refreshGlobalCommunityPosts();
              setCommunityStatus('Community post deleted globally.', 'ok');
            } catch (_) {
              setCommunityStatus('Could not delete the global community post.', 'error');
            }
            return;
          }
          window.open(buildDeleteRequestIssueUrl(post), '_blank', 'noopener');
          setCommunityStatus('Opened a delete request for review.', 'ok');
        });
      });
      if (communityEmpty) communityEmpty.classList.toggle('hidden', posts.length > 0);
    }

    function formatSubmitterLabel(user) {
      if (!user) return '';
      return user.username + ' (' + user.id + ')';
    }

    function renderVerifiedUser() {
      const user = verifiedDiscordUser;
      if (!discordLogin) return;
      discordLogin.href = makeDiscordAuthUrl();
      if (!user) {
        discordLogin.classList.remove('hidden');
        verifiedUser && verifiedUser.classList.add('hidden');
        discordLogout && discordLogout.classList.add('hidden');
        document.querySelectorAll('[data-copy-static]').forEach(function (button) {
          button.classList.add('hidden');
        });
        renderAdminPanel();
        renderReviewQueue();
        return;
      }
      discordLogin.classList.add('hidden');
      if (verifiedName) verifiedName.textContent = formatSubmitterLabel(user);
      if (verifiedMeta) verifiedMeta.textContent = 'Verified with Discord. Your username, ID, and avatar will be attached to the submission.';
      if (verifiedAvatar) {
        verifiedAvatar.src = getDiscordAvatarUrl(user);
      }
      verifiedUser && verifiedUser.classList.remove('hidden');
      discordLogout && discordLogout.classList.remove('hidden');
      document.querySelectorAll('[data-copy-static]').forEach(function (button) {
        button.classList.remove('hidden');
      });
      renderAdminPanel();
      renderCommunityPosts();
      refreshReviewQueue();
    }

    async function resolveDiscordAuth() {
      const hash = new URLSearchParams(window.location.hash.replace(/^#/, ''));
      const accessToken = hash.get('access_token');
      if (!accessToken) {
        verifiedDiscordUser = loadStoredVerifiedUser();
        renderVerifiedUser();
        return;
      }
      try {
        const response = await fetch('https://discord.com/api/users/@me', {
          headers: {
            Authorization: 'Bearer ' + accessToken
          }
        });
        if (!response.ok) {
          throw new Error('Discord verification failed.');
        }
        const profile = await response.json();
        verifiedDiscordUser = {
          id: profile.id,
          username: profile.global_name || profile.username || 'Discord User',
          avatar: profile.avatar || '',
          discriminator: profile.discriminator || '0'
        };
        persistVerifiedUser(verifiedDiscordUser);
        if (window.history && window.history.replaceState) {
          window.history.replaceState({}, document.title, window.location.pathname + window.location.search);
        } else {
          window.location.hash = '';
        }
        renderVerifiedUser();
        setCommunityStatus('Discord verification complete. Your submission will show your username, Discord ID, and profile picture.', 'ok');
      } catch (_) {
        clearVerifiedUser();
        renderVerifiedUser();
        setCommunityStatus('Discord verification failed. Try verifying again.', 'error');
      }
    }

    function getCommunitySubmission() {
      const gameName = String(communityName && communityName.value || '').trim();
      const category = String(communityCategory && communityCategory.value || 'none').trim();
      const categoryLabel = getCategoryLabel(category);
      const shareLink = String(communityLink && communityLink.value || '').trim();
      const note = String(communityNote && communityNote.value || '').trim();
      const shareCode = normalizePrivateCode(shareLink);
      const isShareUrl = /^https:\/\/www\.roblox\.com\/share\?/i.test(shareLink);
      const hasServerType = /[?&]type=Server(?:&|$)/i.test(shareLink);
      const submitterLabel = formatSubmitterLabel(verifiedDiscordUser);
      const submitterAvatar = getDiscordAvatarUrl(verifiedDiscordUser);
      const valid = !!gameName && !!shareCode && isShareUrl && hasServerType && !!verifiedDiscordUser;
      return {
        gameName: gameName,
        category: category,
        categoryLabel: categoryLabel,
        shareLink: shareLink,
        shareCode: shareCode,
        note: note,
        submitterLabel: submitterLabel,
        submitterAvatar: submitterAvatar,
        valid: valid
      };
    }

    function renderCommunityPreview() {
      const submission = getCommunitySubmission();
      if (!communityPreview) return submission;
      if (!submission.shareLink) {
        communityPreview.textContent = 'Enter a valid Roblox private server share link to prepare a submission.';
        if (communitySubmit) communitySubmit.disabled = true;
        setCommunityStatus(verifiedDiscordUser
          ? 'Submissions open a pre-filled GitHub issue for manual review before being added to the public list.'
          : 'Verify with Discord first, then enter a valid Roblox private server share link.');
        return submission;
      }
      if (!verifiedDiscordUser) {
        communityPreview.textContent = 'Verify with Discord first. After that, your username, Discord ID, and avatar will be attached to the submission.';
        if (communitySubmit) communitySubmit.disabled = true;
        setCommunityStatus('Discord verification is required before posting a community private server.', 'error');
        return submission;
      }
      if (!submission.valid) {
        communityPreview.textContent = 'Invalid format. Use a Roblox private server share link like https://www.roblox.com/share?code=...&type=Server and include a game name.';
        if (communitySubmit) communitySubmit.disabled = true;
        setCommunityStatus('Only Roblox private server share-link format is accepted.', 'error');
        return submission;
      }
      communityPreview.textContent = [
        'Submitter: ' + submission.submitterLabel,
        'Game: ' + submission.gameName,
        'Category: ' + submission.categoryLabel,
        'Share code: ' + submission.shareCode,
        'Link: ' + submission.shareLink,
        'Avatar: ' + submission.submitterAvatar,
        submission.note ? 'Note: ' + submission.note : 'Note: (none)'
      ].join('\n');
      if (communitySubmit) communitySubmit.disabled = false;
      setCommunityStatus(isCommunityAdmin(verifiedDiscordUser)
        ? 'Valid verified submission ready. You can submit it for review or publish it globally from the admin menu.'
        : 'Valid verified submission ready. It will be sent for manual review before it appears publicly.', 'ok');
      return submission;
    }

    function applyFilters() {
      const query = String(searchInput && searchInput.value || '').trim().toLowerCase();
      const category = String(categorySelect && categorySelect.value || 'all');
      const sort = String(sortSelect && sortSelect.value || 'popularity-desc');

      const visibleCards = cards.filter(function (card) {
        const name = String(card.getAttribute('data-name') || '').toLowerCase();
        const searchBlob = String(card.getAttribute('data-search') || '').toLowerCase();
        const cardCategory = String(card.getAttribute('data-category') || 'other');
        const matchesQuery = !query || name.includes(query) || searchBlob.includes(query);
        const matchesCategory = category === 'all' || cardCategory === category;
        const visible = matchesQuery && matchesCategory;
        card.classList.toggle('hidden', !visible);
        return visible;
      });

      const sorted = visibleCards.slice().sort(function (a, b) {
        const nameA = String(a.getAttribute('data-name') || '');
        const nameB = String(b.getAttribute('data-name') || '');
        const popA = Number(a.getAttribute('data-popularity') || '0');
        const popB = Number(b.getAttribute('data-popularity') || '0');

        if (sort === 'name-asc') return nameA.localeCompare(nameB);
        if (sort === 'name-desc') return nameB.localeCompare(nameA);
        if (sort === 'popularity-asc') return popA - popB;
        return popB - popA;
      });

      sorted.forEach(function (card) {
        grid.appendChild(card);
      });

      if (emptyState) emptyState.classList.toggle('hidden', visibleCards.length > 0);
    }

    searchInput && searchInput.addEventListener('input', applyFilters);
    categorySelect && categorySelect.addEventListener('change', applyFilters);
    sortSelect && sortSelect.addEventListener('change', applyFilters);
    communityName && communityName.addEventListener('input', function () {
      persistDraft();
      renderCommunityPreview();
    });
    communityCategory && communityCategory.addEventListener('change', function () {
      persistDraft();
      renderCommunityPreview();
    });
    communityLink && communityLink.addEventListener('input', function () {
      persistDraft();
      renderCommunityPreview();
    });
    communityNote && communityNote.addEventListener('input', function () {
      persistDraft();
      renderCommunityPreview();
    });
    communitySubmit && communitySubmit.addEventListener('click', function () {
      const submission = renderCommunityPreview();
      if (!submission.valid) return;
      window.open(buildCommunityIssueUrl(submission), '_blank', 'noopener');
      clearDraft();
      if (communityName) communityName.value = '';
      if (communityCategory) communityCategory.value = 'none';
      if (communityLink) communityLink.value = '';
      if (communityNote) communityNote.value = '';
      communityFormOpen = false;
      renderCommunityFormState();
      renderCommunityPreview();
      setCommunityStatus('Opened a pre-filled GitHub issue for review.', 'ok');
    });
    toggleCommunityForm && toggleCommunityForm.addEventListener('click', function () {
      communityFormOpen = !communityFormOpen;
      renderCommunityFormState();
    });
    toggleAdminMenu && toggleAdminMenu.addEventListener('click', function () {
      adminMenuOpen = !adminMenuOpen;
      renderAdminPanel();
    });
    discordLogin && discordLogin.addEventListener('click', function () {
      persistDraft();
    });
    discordLogout && discordLogout.addEventListener('click', function () {
      clearVerifiedUser();
      renderVerifiedUser();
      renderCommunityPreview();
      setCommunityStatus('Logged out from the saved Discord verification on this browser.', 'ok');
    });
    adminSaveToken && adminSaveToken.addEventListener('click', function () {
      const token = String(adminTokenInput && adminTokenInput.value || '').trim();
      if (!token) {
        setAdminStatus('Enter a GitHub token first.', 'error');
        return;
      }
      persistAdminToken(token);
      setAdminStatus('GitHub token saved in this browser for admin actions.', 'ok');
      refreshReviewQueue();
    });
    adminClearToken && adminClearToken.addEventListener('click', function () {
      clearAdminToken();
      if (adminTokenInput) adminTokenInput.value = '';
      setAdminStatus('Saved GitHub token cleared from this browser.', 'ok');
      refreshReviewQueue();
    });
    adminPublishPost && adminPublishPost.addEventListener('click', async function () {
      if (!isCommunityAdmin(verifiedDiscordUser)) {
        setAdminStatus('Only your admin Discord account can publish global posts.', 'error');
        return;
      }
      const token = String(adminTokenInput && adminTokenInput.value || loadAdminToken()).trim();
      if (!token) {
        setAdminStatus('Paste and save a GitHub token first.', 'error');
        return;
      }
      const submission = renderCommunityPreview();
      if (!submission.valid) {
        setAdminStatus('Prepare a valid verified submission first.', 'error');
        return;
      }
      try {
        const nextPosts = [{
          id: String(Date.now()) + '-' + Math.random().toString(36).slice(2, 8),
          submitterId: verifiedDiscordUser.id,
          submitterLabel: submission.submitterLabel,
          submitterAvatar: submission.submitterAvatar,
          gameName: submission.gameName,
          categoryLabel: submission.categoryLabel,
          category: submission.category,
          shareCode: submission.shareCode,
          shareLink: submission.shareLink,
          note: submission.note
        }].concat(loadCommunityPosts()).slice(0, 100);
        await writeGitHubJson(COMMUNITY_POSTS_PATH, nextPosts, 'Publish community post ' + submission.gameName, token);
        persistAdminToken(token);
        await refreshGlobalCommunityPosts();
        clearDraft();
        if (communityName) communityName.value = '';
        if (communityCategory) communityCategory.value = 'none';
        if (communityLink) communityLink.value = '';
        if (communityNote) communityNote.value = '';
        communityFormOpen = false;
        renderCommunityFormState();
        renderCommunityPreview();
        setAdminStatus('Community post published globally.', 'ok');
      } catch (_) {
        setAdminStatus('Could not publish the global community post. Check the token scope and repo access.', 'error');
      }
    });
    adminDeletePost && adminDeletePost.addEventListener('click', async function () {
      if (!verifiedDiscordUser) {
        setAdminStatus('Verify with Discord first so your posts can be matched to your account.', 'error');
        return;
      }
      const token = String(adminTokenInput && adminTokenInput.value || loadAdminToken()).trim();
      if (!token) {
        setAdminStatus('Paste and save a GitHub token first.', 'error');
        return;
      }
      const wanted = String(adminDeleteNameInput && adminDeleteNameInput.value || '').trim().toLowerCase();
      if (!wanted) {
        setAdminStatus('Type the game name of the post you want to delete first.', 'error');
        return;
      }
      const isAdmin = isCommunityAdmin(verifiedDiscordUser);
      const matches = loadCommunityPosts().filter(function (post) {
        if (!post || String(post.gameName || '').trim().toLowerCase() !== wanted) return false;
        return isAdmin || post.submitterId === verifiedDiscordUser.id;
      });
      if (!matches.length) {
        setAdminStatus(isAdmin
          ? 'No community post found with the game name "' + wanted + '".'
          : 'None of your community posts use the game name "' + wanted + '".', 'error');
        return;
      }
      const nextPosts = loadCommunityPosts().filter(function (post) {
        return matches.indexOf(post) === -1;
      });
      try {
        await writeGitHubJson(COMMUNITY_POSTS_PATH, nextPosts, 'Delete community post(s): ' + matches.map(function (m) { return m.gameName; }).join(', '), token);
        persistAdminToken(token);
        await refreshGlobalCommunityPosts();
        if (adminDeleteNameInput) adminDeleteNameInput.value = '';
        setAdminStatus('Deleted ' + matches.length + ' post(s) named "' + wanted + '".', 'ok');
      } catch (_) {
        setAdminStatus('Could not delete the post(s). Check the token scope and repo access.', 'error');
      }
    });
    applyFilters();
    restoreDraft();
    renderCommunityFormState();
    renderAdminPanel();
    refreshGlobalCommunityPosts();
    resolveDiscordAuth().then(function () {
      renderCommunityPreview();
      refreshReviewQueue();
    });
  }

  async function initChangelogsPage() {
    const list = byId('changelog-list');
    const status = byId('changelog-status');
    if (!list || !status) return;
    try {
      const entries = await fetchJsonFile('data/changelogs.json');
      list.innerHTML = entries.map(function (entry) {
        return '<article class="changelog-entry">'
          + '<div class="game-meta"><span class="pill">' + escapeHtml(entry.date) + '</span></div>'
          + '<h2>' + escapeHtml(entry.title) + '</h2>'
          + '<ul>' + (entry.items || []).map(function (item) {
              return '<li>' + escapeHtml(item) + '</li>';
            }).join('') + '</ul>'
          + '</article>';
      }).join('');
      status.textContent = 'Loaded changelogs.';
      status.className = 'small status ok';
    } catch (_) {
      list.innerHTML = '';
      status.textContent = 'Could not load changelogs right now.';
      status.className = 'small status error';
    }
  }

  if (document.body.dataset.page === 'invite') initInvitePage();
  if (document.body.dataset.page === 'menu') initMenuPage();
  if (document.body.dataset.page === 'ps') initPrivateServersPage();
  if (document.body.dataset.page === 'changelogs') initChangelogsPage();
})();