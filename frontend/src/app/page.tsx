import { PublicNavbar } from "@/components/layout/PublicNavbar";
import { Footer } from "@/components/layout/Footer";
import { Hero } from "@/components/landing/Hero";
import { FeaturedCourses } from "@/components/landing/FeaturedCourses";
import { InstructorCta } from "@/components/landing/InstructorCta";

export default function Home() {
  return (
    <div className="flex flex-1 flex-col">
      <PublicNavbar />
      <main className="flex-1">
        <Hero />
        <FeaturedCourses />
        <InstructorCta />
      </main>
      <Footer />
    </div>
  );
}
