import { useEffect, useMemo, useState } from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import { api, getSession } from './lib/api';
import './certificates.css';

type CertificateSummary = {
  id: string;
  courseId: string;
  courseTitle: string;
  verificationCode: string;
  issuedAtUtc: string;
};

type VerifiedCertificate = {
  verificationCode: string;
  issuedAtUtc: string;
  courseTitle: string;
  student?: {
    firstName: string;
    lastName: string;
  };
};

type CertificateDocument = {
  studentName: string;
  studentId?: string;
  courseTitle: string;
  verificationCode: string;
  issuedAtUtc: string;
  professor?: string;
  academicPeriod?: string;
  projectNote?: string;
  source: 'issued' | 'academic-preview';
};

const ISO700_PREVIEWS: Record<string, CertificateDocument> = {
  jairo: {
    studentName: 'Francis Jairo Matías Rosario',
    studentId: 'A00115261',
    courseTitle: 'Gestión de Sitios Web (ISO-700)',
    verificationCode: 'KOG-ISO700-A00115261',
    issuedAtUtc: '2024-08-31T12:00:00Z',
    professor: 'Delby Acosta Taveras',
    academicPeriod: 'Mayo - Agosto 2024',
    projectNote: 'Proyecto final académico desarrollado dentro de la plataforma Kognia.',
    source: 'academic-preview',
  },
  eliandres: {
    studentName: 'Eliandres Rodríguez Cepeda',
    studentId: 'A00112070',
    courseTitle: 'Gestión de Sitios Web (ISO-700)',
    verificationCode: 'KOG-ISO700-A00112070',
    issuedAtUtc: '2024-08-31T12:00:00Z',
    professor: 'Delby Acosta Taveras',
    academicPeriod: 'Mayo - Agosto 2024',
    projectNote: 'Proyecto final académico desarrollado dentro de la plataforma Kognia.',
    source: 'academic-preview',
  },
  ramon: {
    studentName: 'Ramón Rosario Rodríguez',
    studentId: 'A00110961',
    courseTitle: 'Gestión de Sitios Web (ISO-700)',
    verificationCode: 'KOG-ISO700-A00110961',
    issuedAtUtc: '2024-08-31T12:00:00Z',
    professor: 'Delby Acosta Taveras',
    academicPeriod: 'Mayo - Agosto 2024',
    projectNote: 'Proyecto final académico desarrollado dentro de la plataforma Kognia.',
    source: 'academic-preview',
  },
};

function CertificateNav() {
  return (
    <nav className="certificate-nav" aria-label="Navegación de certificados">
      <Link className="certificate-brand" to="/">
        <span className="certificate-brand-mark" aria-hidden="true">K</span>
        <span>Kognia</span>
      </Link>
      <div>
        <Link to="/dashboard">Dashboard</Link>
        <Link to="/my-learning">Mi aprendizaje</Link>
        <Link className="is-active" to="/certificates">Certificados</Link>
      </div>
    </nav>
  );
}

function formatDate(date: string) {
  return new Intl.DateTimeFormat('es-DO', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(new Date(date));
}

function CertificateArtwork({ document }: { document: CertificateDocument }) {
  return (
    <article className="certificate-sheet" aria-label={`Certificado de ${document.studentName}`}>
      <div className="certificate-corner certificate-corner-tl" />
      <div className="certificate-corner certificate-corner-tr" />
      <div className="certificate-corner certificate-corner-bl" />
      <div className="certificate-corner certificate-corner-br" />
      <div className="certificate-orbit certificate-orbit-left" />
      <div className="certificate-orbit certificate-orbit-right" />

      <header className="certificate-sheet-header">
        <div className="certificate-logo-mark" aria-hidden="true"><span>K</span></div>
        <div>
          <strong>Kognia</strong>
          <small>APRENDE · CONECTA · CRECE</small>
        </div>
      </header>

      <div className="certificate-title-block">
        <p className="certificate-overline">CERTIFICADO DE FINALIZACIÓN</p>
        <p className="certificate-awarded">Otorgado por haber completado satisfactoriamente</p>
        <h1>{document.courseTitle}</h1>
      </div>

      <div className="certificate-recipient">
        <span>PRESENTADO A</span>
        <h2>{document.studentName}</h2>
        {document.studentId && <p><strong>Matrícula:</strong> {document.studentId}</p>}
      </div>

      {document.projectNote && <p className="certificate-project-note">{document.projectNote}</p>}

      <div className="certificate-meta-grid">
        <div>
          <span className="certificate-meta-icon" aria-hidden="true">⌂</span>
          <p><strong>Profesor</strong><br />{document.professor ?? 'Equipo académico Kognia'}</p>
        </div>
        <div>
          <span className="certificate-meta-icon" aria-hidden="true">▦</span>
          <p><strong>Periodo académico</strong><br />{document.academicPeriod ?? formatDate(document.issuedAtUtc)}</p>
        </div>
      </div>

      <p className="certificate-recognition">En reconocimiento a su dedicación, esfuerzo y culminación exitosa del programa.</p>

      <footer className="certificate-signatures">
        <div className="certificate-signature">
          <span className="certificate-signature-script">{document.professor ?? 'Kognia Faculty'}</span>
          <i />
          <strong>Profesor</strong>
          <small>{document.professor ?? 'Kognia'}</small>
        </div>

        <div className="certificate-seal" aria-label="Sello Kognia">
          <div className="certificate-seal-ring">
            <span>K</span>
          </div>
        </div>

        <div className="certificate-signature">
          <span className="certificate-signature-script">Kognia</span>
          <i />
          <strong>Plataforma Kognia</strong>
          <small>Aprende · Conecta · Crece</small>
        </div>
      </footer>

      <div className="certificate-verification-line">
        <span>Certificado verificable</span>
        <strong>{document.verificationCode}</strong>
      </div>
    </article>
  );
}

function CertificateToolbar({ document }: { document: CertificateDocument }) {
  const [copied, setCopied] = useState(false);

  async function copyVerification() {
    await navigator.clipboard.writeText(document.verificationCode);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  }

  async function shareCertificate() {
    const url = window.location.href;
    if (navigator.share) {
      await navigator.share({
        title: `Certificado Kognia - ${document.studentName}`,
        text: `${document.studentName} · ${document.courseTitle}`,
        url,
      });
      return;
    }
    await navigator.clipboard.writeText(url);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1800);
  }

  return (
    <div className="certificate-toolbar no-print">
      <div>
        <span className={`certificate-status ${document.source === 'issued' ? 'is-valid' : 'is-preview'}`}>
          <i />
          {document.source === 'issued' ? 'Certificado emitido' : 'Vista previa académica'}
        </span>
        <p>Código <strong>{document.verificationCode}</strong></p>
      </div>
      <div className="certificate-toolbar-actions">
        <button type="button" onClick={() => void copyVerification()}>{copied ? 'Copiado ✓' : 'Copiar código'}</button>
        <button type="button" onClick={() => void shareCertificate()}>Compartir</button>
        <button className="certificate-primary-action" type="button" onClick={() => window.print()}>Imprimir / PDF</button>
      </div>
    </div>
  );
}

export function CertificatesPage() {
  const session = getSession();
  const [certificates, setCertificates] = useState<CertificateSummary[]>([]);
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!session) return;
    api<CertificateSummary[]>('/api/certificates/me')
      .then(setCertificates)
      .catch(() => setMessage('No fue posible cargar tus certificados.'));
  }, [session]);

  if (!session) return <Navigate to="/login" replace />;

  return (
    <>
      <CertificateNav />
      <main className="certificate-library-shell">
        <section className="certificate-library-hero">
          <div>
            <p className="certificate-kicker">Credenciales Kognia</p>
            <h1>Mis certificados</h1>
            <p>Consulta, comparte e imprime tus logros verificables.</p>
          </div>
          <div className="certificate-library-badge"><span>◆</span><strong>{certificates.length}</strong><small>emitidos</small></div>
        </section>

        {message && <p className="certificate-message" role="alert">{message}</p>}

        <section className="certificate-library-section">
          <div className="certificate-section-heading">
            <div>
              <p className="certificate-kicker">Credenciales oficiales</p>
              <h2>Certificados emitidos</h2>
            </div>
          </div>

          {certificates.length === 0 ? (
            <div className="certificate-empty-state">
              <span>◆</span>
              <h3>Aún no tienes certificados emitidos</h3>
              <p>Completa un curso y aprueba sus evaluaciones para obtener tu primera credencial Kognia.</p>
              <Link to="/discover">Explorar cursos</Link>
            </div>
          ) : (
            <div className="certificate-card-grid">
              {certificates.map((certificate) => (
                <article className="certificate-card" key={certificate.id}>
                  <div className="certificate-card-preview">
                    <span className="certificate-card-seal">K</span>
                    <small>CERTIFICADO DE FINALIZACIÓN</small>
                    <strong>{certificate.courseTitle}</strong>
                    <i>{certificate.verificationCode}</i>
                  </div>
                  <div className="certificate-card-body">
                    <p className="certificate-kicker">Emitido {formatDate(certificate.issuedAtUtc)}</p>
                    <h3>{certificate.courseTitle}</h3>
                    <p>{certificate.verificationCode}</p>
                    <div>
                      <Link to={`/certificates/view/${encodeURIComponent(certificate.verificationCode)}`}>Ver certificado</Link>
                      <Link to={`/certificates/verify/${encodeURIComponent(certificate.verificationCode)}`}>Verificar</Link>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>

        <section className="certificate-library-section certificate-academic-preview no-print">
          <div className="certificate-section-heading">
            <div>
              <p className="certificate-kicker">Demo académica ISO-700</p>
              <h2>Certificados del equipo</h2>
              <p>Vista previa del diseño solicitado para Gestión de Sitios Web.</p>
            </div>
          </div>
          <div className="certificate-preview-grid">
            {Object.entries(ISO700_PREVIEWS).map(([key, certificate]) => (
              <Link to={`/certificates/demo/${key}`} className="certificate-preview-person" key={key}>
                <span className="certificate-avatar">{certificate.studentName.split(' ').map((part) => part[0]).slice(0, 2).join('')}</span>
                <div><strong>{certificate.studentName}</strong><small>{certificate.studentId}</small></div>
                <b>→</b>
              </Link>
            ))}
          </div>
        </section>
      </main>
    </>
  );
}

export function CertificateDetailPage() {
  const session = getSession();
  const { verificationCode } = useParams();
  const [data, setData] = useState<VerifiedCertificate | null>(null);
  const [invalid, setInvalid] = useState(false);

  useEffect(() => {
    if (!verificationCode) return;
    api<{ valid: boolean; certificate: VerifiedCertificate }>(`/api/certificates/verify/${encodeURIComponent(verificationCode)}`)
      .then((result) => setData(result.certificate))
      .catch(() => setInvalid(true));
  }, [verificationCode]);

  if (!session) return <Navigate to="/login" replace />;
  if (!verificationCode) return <Navigate to="/certificates" replace />;

  const document = data ? {
    studentName: data.student ? `${data.student.firstName} ${data.student.lastName}` : 'Estudiante Kognia',
    courseTitle: data.courseTitle,
    verificationCode: data.verificationCode,
    issuedAtUtc: data.issuedAtUtc,
    source: 'issued' as const,
  } : null;

  return (
    <>
      <CertificateNav />
      <main className="certificate-view-shell">
        <div className="certificate-view-back no-print"><Link to="/certificates">← Volver a mis certificados</Link></div>
        {invalid ? (
          <section className="certificate-invalid"><span>!</span><h1>Certificado no encontrado</h1><p>El código indicado no corresponde a una credencial válida de Kognia.</p></section>
        ) : !document ? (
          <section className="certificate-loading">Preparando certificado...</section>
        ) : (
          <>
            <CertificateToolbar document={document} />
            <CertificateArtwork document={document} />
          </>
        )}
      </main>
    </>
  );
}

export function AcademicCertificatePreviewPage() {
  const { studentKey } = useParams();
  const document = useMemo(() => studentKey ? ISO700_PREVIEWS[studentKey] : undefined, [studentKey]);

  if (!document) return <Navigate to="/certificates" replace />;

  return (
    <>
      <CertificateNav />
      <main className="certificate-view-shell">
        <div className="certificate-view-back no-print"><Link to="/certificates">← Volver a certificados</Link></div>
        <CertificateToolbar document={document} />
        <CertificateArtwork document={document} />
      </main>
    </>
  );
}

export function CertificateVerifyPage() {
  const { verificationCode } = useParams();
  const [data, setData] = useState<{ valid: boolean; certificate?: VerifiedCertificate } | null>(null);

  useEffect(() => {
    if (!verificationCode) return;
    api<{ valid: boolean; certificate: VerifiedCertificate }>(`/api/certificates/verify/${encodeURIComponent(verificationCode)}`)
      .then(setData)
      .catch(() => setData({ valid: false }));
  }, [verificationCode]);

  return (
    <>
      <CertificateNav />
      <main className="certificate-verify-shell">
        {!data ? (
          <section className="certificate-verify-card is-loading"><span className="certificate-verify-icon">◌</span><h1>Verificando certificado</h1><p>Consultando el registro de credenciales Kognia...</p></section>
        ) : !data.valid || !data.certificate ? (
          <section className="certificate-verify-card is-invalid"><span className="certificate-verify-icon">×</span><p className="certificate-kicker">Verificación pública</p><h1>Certificado no válido</h1><p>No encontramos una credencial emitida con este código.</p><code>{verificationCode}</code></section>
        ) : (
          <section className="certificate-verify-card is-valid">
            <span className="certificate-verify-icon">✓</span>
            <p className="certificate-kicker">Verificación pública</p>
            <h1>Certificado válido</h1>
            <p className="certificate-verify-lead">Esta credencial fue emitida por Kognia y coincide con nuestros registros.</p>
            <dl>
              <div><dt>Estudiante</dt><dd>{data.certificate.student ? `${data.certificate.student.firstName} ${data.certificate.student.lastName}` : 'Estudiante Kognia'}</dd></div>
              <div><dt>Curso</dt><dd>{data.certificate.courseTitle}</dd></div>
              <div><dt>Emitido</dt><dd>{formatDate(data.certificate.issuedAtUtc)}</dd></div>
              <div><dt>Código</dt><dd>{data.certificate.verificationCode}</dd></div>
            </dl>
            <div className="certificate-authenticity"><span>K</span><div><strong>Autenticidad confirmada</strong><small>Registro oficial de Kognia</small></div></div>
          </section>
        )}
      </main>
    </>
  );
}