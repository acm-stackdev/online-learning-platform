import { Hero } from "@/components/landing/Hero";
import { FeaturedCourses } from "@/components/landing/FeaturedCourses";
import { InstructorCta } from "@/components/landing/InstructorCta";

export default function Home() {
  return (
    <>
      <Hero />
      <FeaturedCourses />
      <InstructorCta />
    </>
  );
}
