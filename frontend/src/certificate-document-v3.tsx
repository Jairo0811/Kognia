import { useEffect, useMemo, useState } from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import { api, getSession } from './lib/api';
import './certificate-document-v3.css';

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

function formatDate(date: string) {
  return new Intl.DateTimeFormat('es-DO', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(new Date(date));
}

function CertificateV3Nav() {
  return (
    <nav className="certificate-v3-nav no-print" aria-label="Navegación de certificados">
      <Link className="certificate-v3-nav-brand" to="/">
        <img src="/branding/kognia-logo.png" alt="Kognia" />
      </Link>
      <div>
        <Link to="/dashboard">Dashboard</Link>
        <Link to="/my-learning">Mi aprendizaje</Link>
        <Link className="is-active" to="/certificates">Certificados</Link>
      </div>
    </nav>
  );
}

function CertificateV3Toolbar({ document }: { document: CertificateDocument }) {
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
    <div className="certificate-v3-toolbar no-print">
      <div>
        <span className={`certificate-v3-status ${document.source === 'issued' ? 'is-valid' : 'is-preview'}`}>
          <i />
          {document.source === 'issued' ? 'Certificado emitido' : 'Vista previa académica'}
        </span>
        <p>Código <strong>{document.verificationCode}</strong></p>
      </div>
      <div className="certificate-v3-toolbar-actions">
        <button type="button" onClick={() => void copyVerification()}>{copied ? 'Copiado ✓' : 'Copiar código'}</button>
        <button type="button" onClick={() => void shareCertificate()}>Compartir</button>
        <button className="certificate-v3-primary" type="button" onClick={() => window.print()}>Imprimir / PDF</button>
      </div>
    </div>
  );
}

function CertificateV3Artwork({ document }: { document: CertificateDocument }) {
  return (
    <article className="certificate-v3-sheet" aria-label={`Certificado de ${document.studentName}`}>
      <div className="certificate-v3-frame" aria-hidden="true" />
      <div className="certificate-v3-corner corner-tl" aria-hidden="true" />
      <div className="certificate-v3-corner corner-tr" aria-hidden="true" />
      <div className="certificate-v3-corner corner-bl" aria-hidden="true" />
      <div className="certificate-v3-corner corner-br" aria-hidden="true" />
      <div className="certificate-v3-wave wave-left" aria-hidden="true" />
      <div className="certificate-v3-wave wave-right" aria-hidden="true" />

      <header className="certificate-v3-header">
        <p className="certificate-v3-corner-copy copy-left">CONOCIMIENTO<br />QUE CONECTA<br />OPORTUNIDADES</p>
        <img className="certificate-v3-logo" src="/branding/kognia-logo-certificate.svg" alt="Kognia — Learn, Connect, Grow" />
        <p className="certificate-v3-corner-copy copy-right">EDUCACIÓN<br />PARA UN<br />FUTURO REAL</p>
      </header>

      <section className="certificate-v3-main">
        <div className="certificate-v3-title-row">
          <span aria-hidden="true" />
          <h1>CERTIFICADO DE FINALIZACIÓN</h1>
          <span aria-hidden="true" />
        </div>
        <p className="certificate-v3-awarded">Otorgado por haber completado satisfactoriamente la materia</p>

        <div className="certificate-v3-course-ribbon">
          <span aria-hidden="true">◇</span>
          <strong>{document.courseTitle}</strong>
          <span aria-hidden="true">◇</span>
        </div>

        <div className="certificate-v3-presented">
          <i aria-hidden="true" />
          <span>PRESENTADO A</span>
          <i aria-hidden="true" />
        </div>

        <h2 className="certificate-v3-recipient">{document.studentName}</h2>
        {document.studentId && (
          <p className="certificate-v3-student-id"><strong>Matrícula:</strong> {document.studentId}</p>
        )}
        {document.projectNote && <p className="certificate-v3-project-note">{document.projectNote}</p>}

        <div className="certificate-v3-meta">
          <div className="certificate-v3-meta-item">
            <span className="certificate-v3-meta-icon" aria-hidden="true">♙</span>
            <p><strong>Profesor:</strong><br />{document.professor ?? 'Equipo académico Kognia'}</p>
          </div>
          <div className="certificate-v3-meta-divider" aria-hidden="true" />
          <div className="certificate-v3-meta-item">
            <span className="certificate-v3-meta-icon" aria-hidden="true">▣</span>
            <p><strong>Periodo académico:</strong><br />{document.academicPeriod ?? formatDate(document.issuedAtUtc)}</p>
          </div>
        </div>

        <p className="certificate-v3-recognition">
          En reconocimiento a su dedicación, esfuerzo y culminación exitosa de la asignatura.
        </p>
      </section>

      <footer className="certificate-v3-footer">
        <div className="certificate-v3-microcopy micro-left" aria-hidden="true">
          APRENDER<br />CONECTAR<br />CRECER
        </div>

        <div className="certificate-v3-signature professor-signature">
          <span className="certificate-v3-signature-script">{document.professor ?? 'Kognia Faculty'}</span>
          <i />
          <strong>Profesor</strong>
          <small>{document.professor ?? 'Kognia'}</small>
        </div>

        <div className="certificate-v3-seal" aria-label="Sello oficial Kognia">
          <span className="certificate-v3-seal-stars">✦ KOGNIA ✦</span>
          <div className="certificate-v3-seal-core">
            <img src="/branding/kognia-isotipo-certificate.svg" alt="" aria-hidden="true" />
          </div>
        </div>

        <div className="certificate-v3-signature platform-signature">
          <span className="certificate-v3-signature-script">Kognia</span>
          <i />
          <strong>Plataforma Kognia</strong>
          <small>LEARN · CONNECT · GROW</small>
        </div>

        <div className="certificate-v3-microcopy micro-right" aria-hidden="true">
          PERSONAS<br />IDEAS<br />RESULTADOS
        </div>
      </footer>

      <div className="certificate-v3-code">
        <span>Código:</span>
        <strong>{document.verificationCode}</strong>
      </div>
    </article>
  );
}

export function CertificateDetailPageV3() {
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

  const document: CertificateDocument | null = data ? {
    studentName: data.student ? `${data.student.firstName} ${data.student.lastName}` : 'Estudiante Kognia',
    courseTitle: data.courseTitle,
    verificationCode: data.verificationCode,
    issuedAtUtc: data.issuedAtUtc,
    source: 'issued',
  } : null;

  return (
    <>
      <CertificateV3Nav />
      <main className="certificate-v3-shell">
        <div className="certificate-v3-back no-print"><Link to="/certificates">← Volver a mis certificados</Link></div>
        {invalid ? (
          <section className="certificate-v3-invalid">
            <span>!</span>
            <h1>Certificado no encontrado</h1>
            <p>El código indicado no corresponde a una credencial válida de Kognia.</p>
          </section>
        ) : !document ? (
          <section className="certificate-v3-loading">Preparando certificado...</section>
        ) : (
          <>
            <CertificateV3Toolbar document={document} />
            <CertificateV3Artwork document={document} />
          </>
        )}
      </main>
    </>
  );
}

export function AcademicCertificatePreviewPageV3() {
  const { studentKey } = useParams();
  const document = useMemo(() => studentKey ? ISO700_PREVIEWS[studentKey] : undefined, [studentKey]);

  if (!document) return <Navigate to="/certificates" replace />;

  return (
    <>
      <CertificateV3Nav />
      <main className="certificate-v3-shell">
        <div className="certificate-v3-back no-print"><Link to="/certificates">← Volver a certificados</Link></div>
        <CertificateV3Toolbar document={document} />
        <CertificateV3Artwork document={document} />
      </main>
    </>
  );
}
